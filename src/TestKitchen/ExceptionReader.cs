using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TypeKitchen.Internal;

namespace TestKitchen
{
    internal class ExceptionReader : MsilReader
    {
        public ExceptionReader(MemberInfo member) : base(member)
        {
            ExceptionsHandled = new Dictionary<Type, Type>();
            ExceptionsThrown = new Dictionary<Type, Type>();
            
            FindExceptionsHandled(member);
            FindExceptionsThrown(member);
        }

        public Dictionary<Type, Type> ExceptionsHandled { get; set; }
        
        public Dictionary<Type, Type> ExceptionsThrown { get; set; }
        
        public int NumberOfTryBlocks { get; set; }

        private void FindExceptionsThrown(MemberInfo mi)
        {
            if (Body == null)
                return;

            var declaring = mi.DeclaringType;
            var instructions = Instructions.OrderBy(i => i.Offset);
            var sorted = new LinkedList<MsilInstruction>(instructions);
            
            foreach (var thrown in Instructions.Where(i => i.OpCode == OpCodes.Throw))
            {
                if (ExceptionsThrown.ContainsKey(declaring ?? throw new InvalidOperationException()))
                {
                    continue;
                }

                var located = sorted.Find(thrown);

                var previous = located?.Previous;

                // Should be the next type on the stack
                var getter = previous?.Value;
                if(getter == null)
                {
                    continue;
                }

                var code = getter.OpCode;
                if (code != OpCodes.Ldloc_0 &&
                    code != OpCodes.Ldloc_1 &&
                    code != OpCodes.Ldloc_2 &&
                    code != OpCodes.Ldloc_3 &&
                    code != OpCodes.Ldloc_S &&
                    code != OpCodes.Newobj)
                {
                    continue;
                }

                // For all the pickles; what exception was it?
                MsilInstruction setter = null;
                if (code == OpCodes.Ldloc_0)
                {
                    setter = Instructions.SingleOrDefault(i => i.OpCode == OpCodes.Stloc_0);
                }
                else if (code == OpCodes.Ldloc_1)
                {
                    setter = Instructions.SingleOrDefault(i => i.OpCode == OpCodes.Stloc_1);
                }
                else if (code == OpCodes.Ldloc_2)
                {
                    setter = Instructions.SingleOrDefault(i => i.OpCode == OpCodes.Stloc_2);
                }
                else if (code == OpCodes.Ldloc_3)
                {
                    setter = Instructions.SingleOrDefault(i => i.OpCode == OpCodes.Stloc_3);
                }
                else if (code == OpCodes.Ldloc_S)
                {
                    var setters = Instructions.Where(i => i.OpCode == OpCodes.Stloc_S);
                    setter = setters.SingleOrDefault(s => s.Operand.Equals(getter.Operand));
                }
                else if (code == OpCodes.Newobj)
                {
                    var operand = getter.Operand as MemberInfo;
                    if (operand == null)
                    {
                        continue;
                    }

                    var exception = operand.DeclaringType;

                    ExceptionsThrown.Add(declaring, exception);
                }

                if (setter == null)
                    continue;

                FindExceptionThrown(declaring, sorted, setter);
            }
        }

        private void FindExceptionThrown(Type declaring, LinkedList<MsilInstruction> sorted, MsilInstruction setter)
        {
            if(Body == null || setter == null)
                return;

            var located = sorted.Find(setter);

            var throwing = located?.Previous;
            if (throwing == null)
                return;

            var operand = throwing.Value.Operand as MemberInfo;
            if (operand == null)
                return; // really an error

            var exception = operand.DeclaringType;

            ExceptionsThrown.Add(declaring, exception);
        }

        private void FindExceptionsHandled(MemberInfo mi)
        {
            if (Body == null)
            {
                return;
            }

            var declaring = mi.DeclaringType;

            foreach (var clause in Body.ExceptionHandlingClauses)
            {
                NumberOfTryBlocks++;

                var exception = clause.CatchType;
                if(exception == null)
                {
                    continue;
                }

                if(!exception.IsSubclassOf(typeof(Exception)))
					continue;

				if (ExceptionsHandled.ContainsKey(declaring ?? throw new InvalidOperationException()))
                {
                    continue;
                }
                
                ExceptionsHandled.Add(declaring, exception);
            }
        }
    }
}