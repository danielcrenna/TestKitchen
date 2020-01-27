using System;

namespace TestKitchen.Tests
{
	public class BasicTests
    {
        public BasicTests(TestContext context)
        {
            
        }

        public bool Hello_world() => true;

        public bool Test_is_skipped(TestContext context)
        {
            return context.Skip("skippy as I wanna be");
        }

        public bool Two_string_instances_are_equal(TestContext context)
        {
            return "aaa".Equals("AAA", StringComparison.OrdinalIgnoreCase);
        }

        public bool Handles_exceptions()
        {
            try
            {
                throw new ArgumentException();
            }
            catch
            {
                return true;
            }
        }
    }
}
