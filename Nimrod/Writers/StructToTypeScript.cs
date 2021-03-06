﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nimrod.Writers
{
    /// <summary>
    /// For classes which extends String, ie: which are Serializable
    /// </summary>
    public class StructToTypeScript : ToTypeScript
    {
        public override ObjectType ObjectType => ObjectType.Struct;

        public StructToTypeScript(TypeScriptType type, bool strictNullCheck)
            : base(type, strictNullCheck)
        {
            if (!this.Type.Type.IsValueType)
            {
                throw new ArgumentException($"{this.Type.Name} is not a Struct.", nameof(type));
            }
        }

        public override IEnumerable<Type> GetImports() => new List<Type>();

        public override IEnumerable<string> GetLines() => new[] {
            $"export class {this.Type} extends String {{}}"
        };
    }
}
