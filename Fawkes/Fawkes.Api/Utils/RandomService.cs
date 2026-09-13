namespace Fawkes.Api.Utils
{
    /// <summary>
    /// Generates random numbers for (non-cryptographic) purposes. This service is registered as a singleton, so it can be injected into other services or controllers.
    /// </summary>
    public class RandomService
    {
        private readonly Random random;

        public RandomService()
        {
            random = new Random();
        }

        public RandomService(int seed) 
        {
            random = new Random(seed);
        }


        public int Next(int maxValue)
        {
            return random.Next(maxValue);
        }


    }
}
