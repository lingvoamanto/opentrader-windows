using System;
using System.Collections.Generic;

namespace OpenTrader
{
    public class Phenotype
    {
        public double[] Chromosomes;
        private List<StrategyParameter> Parameters;
        public double Fitness { get; set; }

        public static Phenotype Reproduce(Phenotype father, Phenotype mother, double mutation)
        {
            if (father.Chromosomes.Length == mother.Chromosomes.Length)
            {
                Phenotype child = new Phenotype();
                child.Parameters = father.Parameters;

                child.Chromosomes = new double[father.Chromosomes.Length];

                // Randomly copy the chromosomes over
                for (int i = 0; i < child.Chromosomes.Length; i++)
                {
                    Random random = new Random();
                    if (random.NextDouble() > mutation)
                    {
                        bool fatherschromosomes = random.NextDouble() < 0.5;
                        if (fatherschromosomes)
                            child.Chromosomes[i] = father.Chromosomes[i];
                        else
                            child.Chromosomes[i] = mother.Chromosomes[i];
                    }
                    else
                    {
                        child.Chromosomes[i] = child.Mutate(i);
                    }
                }

                return child;
            }
            else
                return null;
        }

        internal bool Matches(Phenotype test)
        {
            if (Chromosomes.Length != test.Chromosomes.Length)
                return false;

            bool matches = true;
            for (int i = 0; i < Chromosomes.Length; i++)
                matches &= double.Equals(Chromosomes[i], test.Chromosomes[i]);

            return matches;
        }


        private double Mutate(int i)
        {
            double range = (Parameters[i].Stop - Parameters[i].Start) / Parameters[i].Step;
            Random random = new Random();
            double chromosome = Math.Round(random.NextDouble() * range);
            chromosome = chromosome * Parameters[i].Step + Parameters[i].Start;
            return chromosome;
        }

        public static Phenotype Seed(List<StrategyParameter> parameters)
        {
            Phenotype seed = new Phenotype();
            seed.Parameters = parameters;
            seed.Chromosomes = new double[parameters.Count];

            // Perform random mutation on each chromosome
            for (int i = 0; i < parameters.Count; i++)
            {
                seed.Chromosomes[i] = seed.Mutate(i);
            }

            return seed;
        }
    }
}
