using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TestKitchen
{
    public class ExceptionAnalyzer
    {
        private readonly ExceptionReader _reader;

        public ExceptionAnalyzer(MemberInfo member)
        {
            _reader = new ExceptionReader(member);
        }

        public IEnumerable<Type> GetReferencedTypes()
        {
            return _reader.Graph != null ? _reader.Graph.Keys.AsEnumerable() 
                       : new List<Type>();
        }

        public IEnumerable<Type> GetExceptionsHandled()
        {
            return _reader.ExceptionsHandled.Values;
        }

        public IEnumerable<Type> GetExceptionsThrown()
        {
            return _reader.ExceptionsThrown.Values;
        }

        public int GetNumberOfTryBlocks()
        {
            return _reader.NumberOfTryBlocks;
        }

        public IEnumerable<Type> GetExceptionsUnhandled()
        {
            var handled = GetExceptionsHandled();
            var thrown = GetExceptionsThrown();

            return thrown.Where(exception => !handled.Contains(exception));
        }
    }
}