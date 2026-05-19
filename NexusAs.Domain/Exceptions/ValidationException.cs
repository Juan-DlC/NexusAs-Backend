using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Domain.Exceptions
{
    public class ValidationException : Exception
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("Ocurrieron uno o más errores de validación.")
        {
            Errors = new Dictionary<string, string[]>(errors);
        }
    }
}
