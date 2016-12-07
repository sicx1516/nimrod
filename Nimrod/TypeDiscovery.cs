﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace Nimrod
{
    public static class TypeDiscovery
    {
        public static HashSet<Type> EnumerateTypes(Type startType) => EnumerateTypes(new[] { startType });
        /// <summary>
        /// Get every type referenced by this type,
        /// including itself, Properties, Generics and Inheritance
        /// The list is guarranted to be unique
        /// </summary>
        // side note: the magic happens here : https://blogs.msdn.microsoft.com/wesdyer/2007/01/25/function-memoization/
        // Func<int, int> fib = null;
        // fib = n => n > 1 ? fib(n – 1) + fib(n – 2) : n;
        // fib = fib.Memoize();
        public static HashSet<Type> EnumerateTypes(IEnumerable<Type> startTypes)
        {
            Func<Type, List<Type>> memoizedEnumerateTypes = null;
            memoizedEnumerateTypes = type =>
            {
                //   try
                //  {

                var referencedTypes = type.ReferencedTypes();
                var recursiveReferencedTypes = referencedTypes.SelectMany(memoizedEnumerateTypes);
                return referencedTypes.Where(t => !t.IsSystem())
                                      .Union(recursiveReferencedTypes).ToList();
                /*    }
                    catch (FileNotFoundException fileNotFoundException)
                    {
                        // during reflection, a type could be found without finding is corrsponding DLLs for reference
                        string message = $@"
    Cannot understand the property of type {type.FullName}.
    The following DLL has not been found in the loaded assemblies: {fileNotFoundException.FileName}
    You should check that the DLLs exists in the folder, and version numbers are the sames.";
                        throw new FileNotFoundException(message, fileNotFoundException.FileName, fileNotFoundException);
                    }*/
            };

            // provide a default value
            // a reference loop is possible if two types references one another
            memoizedEnumerateTypes = memoizedEnumerateTypes.Memoize(new List<Type>());

            return startTypes.SelectMany(memoizedEnumerateTypes).ToHashSet();
        }

        /// <summary>
        /// Returns a set of references types:
        ///  - generics
        ///  - inheritance
        ///  - public properties
        /// </summary>
        public static HashSet<Type> ReferencedTypes(this Type type)
        {
            var generics = type.GetGenericArguments();
            if (type.IsSystem())
            {
                // if the type is system, we only want to seek the generics that can belongs to the caller
                return generics.ToHashSet();
            }
            else
            {
                var baseTypes = type.GetBaseTypes();
                var properties = System.Reflection.TypeExtensions.GetProperties(type, BindingFlags.Public | BindingFlags.Instance)
                    // properties (filter indexers http://stackoverflow.com/questions/1347936/indentifying-a-custom-indexer-using-reflection-in-c-sharp)
                    .Where(p =>
                    {
                        try
                        {
                            var aa = p;
                            var bb = p.GetIndexParameters();
                            return p.GetIndexParameters().Length == 0;
                        }

                        catch (FileNotFoundException fileNotFoundException)
                        {
                            // during reflection, a type could be found without finding is corrsponding DLLs for reference
                            string message = $@"
Cannot understand the property {p.Name} of type {type.FullName}.
The following DLL has not been found in the loaded assemblies: {fileNotFoundException.FileName}
You should check that the DLLs exists in the folder, and version numbers are the sames.";
                            throw new FileNotFoundException(message, fileNotFoundException.FileName, fileNotFoundException);
                        }
                    })
                    .Select(t => t.PropertyType);
                return generics.Union(properties).Union(baseTypes).Union(type).ToHashSet();
            }
        }

        static public IEnumerable<Type> GetWebControllers(IEnumerable<Assembly> assemblies)
            => assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                         .Where(type => type.IsWebController());


        static public IEnumerable<MethodInfo> GetWebControllerActions(this Type controllerType)
        {
            if (!controllerType.IsWebController())
            {
                throw new ArgumentOutOfRangeException($"Type {controllerType.Name} MUST extends System.Web.Mvc.Controller or System.Web.Http.IHttpControler", nameof(controllerType));
            }
            return controllerType.GetMethods()
                                 .Where(method => method.FirstOrDefaultHttpMethodAttribute().HasValue);
        }

    }
}
