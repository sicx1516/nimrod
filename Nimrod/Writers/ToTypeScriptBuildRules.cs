﻿using Nimrod.Writers.Default;
using Nimrod.Writers.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nimrod
{
    public abstract class ToTypeScriptBuildRules
    {
        public abstract Func<TypeScriptType, bool, ToTypeScript> ControllerBuilder { get; }
        public abstract Func<TypeScriptType, bool, ToTypeScript> EnumBuilder { get; }
        public abstract Func<TypeScriptType, bool, ToTypeScript> StructBuilder { get; }
        public abstract Func<TypeScriptType, bool, ToTypeScript> ModelBuilder { get; }
        public abstract StaticToTypeScript StaticBuilder { get; }

        public IEnumerable<ToTypeScriptBuildRule> Rules => new[] {
                new ToTypeScriptBuildRule(type => type.Type.IsWebController(), ControllerBuilder),
                new ToTypeScriptBuildRule(type => type.Type.IsEnum, EnumBuilder),
                new ToTypeScriptBuildRule(type => type.Type.IsValueType, StructBuilder),
                new ToTypeScriptBuildRule(type => true, ModelBuilder)
            };
        public ToTypeScript GetToTypeScript(TypeScriptType type, bool strictNullCheck)
        {
            var item = this.Rules
                    .Where(s => s.Predicate(type))
                    .Select(s => s.Builder(type, strictNullCheck))
                    .FirstOrDefault();
            if (item == null)
            {
                throw new NotImplementedException("Unable to build ToTypeScript object, object doesn't respect any rule");
            }
            return item;
        }

        public static ToTypeScriptBuildRules GetRules(ModuleType moduleType)
        {
            switch (moduleType)
            {
                case ModuleType.Module: return new ToModuleTypeScriptBuildRules();
                case ModuleType.TypeScript: return new ToDefaultTypeScriptBuildRules();
                default: throw new NotImplementedException($"The module type [{moduleType}] doesn't have build rules. You need to write a new class inheriting ToTypeScriptBuildRules");
            }
        }
    }
}
