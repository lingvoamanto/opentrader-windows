using System;
using System.Collections.Generic;
using System.Reflection;
#if __MACOS__
using Foundation;
using AppKit;
#endif

namespace OpenTrader
{
    public class Optimisation
    {
        public List<Phenotype> GenePool;
        public List<Phenotype> Offspring;
        public List<StrategyParameter> Parameters;

        private TraderBook traderbook;

        int GenePoolSize;
        double ReproductionRate;
        int OffspringSize;
        double MutationRate;
        int MaxGenerations;
        MethodInfo? fitnessMethod;
        PropertyInfo? fitnessProperty;



        public Optimisation(TraderBook traderbook)
        {
            this.traderbook = traderbook;
            Parameters = traderbook.TraderScript.StrategyParameters;

            try
            {
                GenePoolSize = traderbook.intProperty("GenePoolSize");
                ReproductionRate = traderbook.doubleProperty("ReproductionRate");
                OffspringSize = (int)(GenePoolSize * ReproductionRate);
                MutationRate = traderbook.doubleProperty("MutationRate");
                MaxGenerations = traderbook.intProperty("MaxGenerations");
            }
            catch
            {
                throw new Exception(); 
            }

            fitnessMethod = traderbook.TraderScript.GetType().GetMethod("GetFitness", new Type[] { typeof(Phenotype) });
            if(fitnessMethod != null && fitnessMethod.ReturnType != typeof(double))
                throw new Exception();

            fitnessProperty = traderbook.TraderScript.GetType().GetProperty("Fitness");
            if (fitnessProperty!= null && fitnessProperty.PropertyType != typeof(double))
                throw new Exception();

            if ( fitnessMethod == null && fitnessProperty == null )
                throw new Exception();

            GenePool = new List<Phenotype>();
            Offspring = new List<Phenotype>();
        }

        public void Optimise()
        {
            // Remember the cursor, and then change it to a watch
            // Gdk.CursorType storedCursorType = traderbook.TraderParent.CursorType;
            // traderbook.TraderParent.CursorType = Gdk.CursorType.Watch;

            try
            {
                Seed();
                for (int generation = 0; generation < MaxGenerations; generation++)
                {
                    NextGeneration();

                    SelectFittest();
                    // System.GC.Collect();
                    traderbook.LogWriteLine("Optimised generation " + generation.ToString());
#if __MACOS__
                    NSRunLoop.Current.RunUntil(NSDate.Now.AddSeconds(1.0));
#endif
                }

                // Now tell the best one
                GetFitness(GenePool[0]);
            }
            catch(Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;

                traderbook.LogWriteLine("Error optimising (Optimise) " + message);
            }

            // Put the cursor back, it was probably an arrow
            // traderbook.TraderParent.CursorType = storedCursorType;
        }




        public void SelectFittest()
        {
#if __MACOS__
            if ((NSApplication.SharedApplication.Delegate as AppDelegate).IsProfiling)
                (NSApplication.SharedApplication.Delegate as AppDelegate).ProfileStack.Push(
                "Optimisation", "SelectFittest");
#endif
#if __WINDOWS__
            if (MainWindow.IsProfiling)
                MainWindow.ProfileStack.Push("Optimisation", "SelectFittest");
#endif
            traderbook.LogWriteLine("(SelectFittest) Begin" );
            List<Phenotype> NewPopulation = new List<Phenotype>();

            foreach (Phenotype individual in GenePool)
            {
                int index = NewPopulation.FindIndex(p => p.Matches(individual));
                if (index == -1)
                {
                    index = NewPopulation.FindIndex(p => p.Fitness < individual.Fitness);
                    if (index == -1)
                        NewPopulation.Add(individual);
                    else
                        NewPopulation.Insert(index, individual);
                }

            }

            foreach (Phenotype individual in Offspring)
            {
                int index = NewPopulation.FindIndex(p => p.Matches(individual));
                if (index == -1)
                {
                    index = NewPopulation.FindIndex(p => p.Fitness < individual.Fitness);
                    if (index == -1)
                        NewPopulation.Add(individual);
                    else
                        NewPopulation.Insert(index, individual);
                }
            }

            int Count = Math.Min(GenePoolSize, NewPopulation.Count);
            GenePool.Clear();
            for (int i = 0; i < Count; i++)
                GenePool.Add(NewPopulation[i]);

            traderbook.LogWriteLine("(SelectFittest) End");

#if __MACOS__
            if ((NSApplication.SharedApplication.Delegate as AppDelegate).IsProfiling)
                (NSApplication.SharedApplication.Delegate as AppDelegate).ProfileStack.Pop();
#endif
#if __WINDOWS__
            if (MainWindow.IsProfiling)
                MainWindow.ProfileStack.Pop();
#endif
        }

        public void Seed()
        {
            traderbook.LogWriteLine("(Seed) Begin");
            // Seed them all
            for (int i = 0; i < GenePoolSize;)
            {
                Phenotype individual = Phenotype.Seed(Parameters);

                int index = GenePool.FindIndex(p => p.Matches(individual));
                if (index == -1)
                {
                    GetFitness(individual);
                    if (!double.IsNaN(individual.Fitness))
                    {
                        GenePool.Add(individual);
                    }
                }
                i++;
            }
            traderbook.LogWriteLine("(Seed) End");
            // Order by fitness
        }

        public void GetFitness(Phenotype phenotype)
        {
            // traderbook.WriteLog("(GetFitness) Begin");


            traderbook.ClearCache();
            if (fitnessMethod != null)
            {
                phenotype.Fitness = (double) fitnessMethod.Invoke(traderbook.TraderScript, new object[] { phenotype});
            }
            else if (fitnessProperty != null) 
            {
                int count = Parameters.Count;

                for (int i = 0; i < count; i++)
                {
                    Parameters[i].Value = phenotype.Chromosomes[i];
                }
                phenotype.Fitness = (double) fitnessProperty.GetValue(traderbook.TraderScript, null);
            }
#if __MACOS__
            NSRunLoop.Current.RunUntil(NSDate.Now.AddSeconds(.15));
#endif
            traderbook.LogWriteLine("(GetFitness) End");
        }

        public void ShowSolution()
        {
            // traderbook.Section.AppendPage( new OptimisationView(Parameters,GenePool), new Label("Genetic") ); 
#if __WINDOWS__

#endif
        }

        public double TotalFitness
        {
            get
            {
                double totalfitness = 0;
                for (int i = 0; i < GenePool.Count; i++)
                {
                    totalfitness += GenePool[i].Fitness;
                }
                return totalfitness;
            }
        }

        public Phenotype FindWeighted(double index)
        {
            double thisindex = 0;
            for (int i = 0; i < GenePool.Count; i++)
            {
                if (thisindex < index)
                    return GenePool[i];
                thisindex += GenePool[i].Fitness;
            }
            return GenePool[GenePool.Count - 1];
        }

        public void NextGeneration()
        {
#if __MACOS__
            if ((NSApplication.SharedApplication.Delegate as AppDelegate).IsProfiling)
                (NSApplication.SharedApplication.Delegate as AppDelegate).ProfileStack.Push(
                "Optimisation", "NextGeneration");
#endif
#if __WINDOWS__
            if (MainWindow.IsProfiling)
                MainWindow.ProfileStack.Push("Optimisation", "NextGeneration");
#endif
            traderbook.LogWrite("(NextGeneration) Begin");
            double totalfitness = TotalFitness;

            for (int i = 0; i < OffspringSize;)
            {
                // choose a couple of random parents
                Random random = new Random();
                // Phenotype father = FindWeighted( random.NextDouble() * totalfitness );
                // Phenotype mother = FindWeighted( random.NextDouble() * totalfitness );			
                Phenotype father = GenePool[(int)(random.NextDouble() * (GenePool.Count - 1.0))];
                Phenotype mother = GenePool[(int)(random.NextDouble() * (GenePool.Count - 1.0))];

                Phenotype offspring = Phenotype.Reproduce(father, mother, MutationRate);
                int index = Offspring.FindIndex(p => p.Matches(offspring));
                if (index == -1)
                {
                    try
                    {
                        GetFitness(offspring);
                        if (!double.IsNaN(offspring.Fitness))
                        {
                            Offspring.Add(offspring);
                            i++;
                        }
                    }
                    catch (Exception e)
                    {
                        traderbook.LogWriteLine("Error optimising (NextGeneration) "+e.Message);                   
                    }
                }
            }
            // traderbook.WriteLog("(NextGeneration) End");
#if __MACOS__
            if ((NSApplication.SharedApplication.Delegate as AppDelegate).IsProfiling)
                (NSApplication.SharedApplication.Delegate as AppDelegate).ProfileStack.Pop();
#endif
#if __WINDOWS__
            if (MainWindow.IsProfiling)
                MainWindow.ProfileStack.Pop();
#endif
        }
    }

    
}