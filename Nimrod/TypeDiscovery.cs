﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nimrod
{
    public static class TypeDiscovery
    {
        static public IEnumerable<Type> EnumerateTypes(Type forType, HashSet<Type> typesEnumerated)
        {
            if (!typesEnumerated.Contains(forType))
            {
                typesEnumerated.Add(forType);
                if (forType == typeof(string))
                {
                    // string is a reference type, but we don't want to generate property type Length
                    yield return forType;
                }
                else
                {
                    yield return forType;

                    foreach (var genericArgumentType in forType.GetGenericArguments())
                    {
                        yield return genericArgumentType;
                        foreach (var subType in EnumerateTypes(genericArgumentType, typesEnumerated))
                        {
                            yield return subType;
                        }
                    }

                    foreach (var property in forType.GetProperties())
                    {
                        foreach (var type in EnumerateTypes(property.PropertyType, typesEnumerated))
                        {
                            yield return type;
                        }
                    }
                }
            }
        }

        static public IEnumerable<Type> GetControllerTypes(IEnumerable<Assembly> assemblies, bool expandDependencies)
        {
            var typesEnumerated = new HashSet<Type>();
            foreach (var assembly in assemblies)
            {
                var controllers = assembly.GetExportedTypes().Where(type => type.IsWebMvcController());
                foreach (var controller in controllers)
                {
                    foreach (var actionType in GetControllerTypes(controller, expandDependencies, typesEnumerated))
                    {
                        yield return actionType;
                    }
                }
            }
        }

        static public IEnumerable<Type> GetControllerTypes(Type controller, bool expandDependencies, HashSet<Type> typesEnumerated)
        {
            var actions = GetControllerActions(controller).ToList();
            if (actions.Any())
            {
                // controller are take into account only if at least one action is typescriptable 
                yield return controller;
                if (expandDependencies)
                {
                    foreach (var method in actions)
                    {
                        foreach (var actionType in GetControllerActionParameterTypes(method, typesEnumerated))
                        {
                            yield return actionType;
                        }
                    }
                }
            }
        }

        static public IEnumerable<Type> GetControllerActionParameterTypes(MethodInfo method, HashSet<Type> typesEnumerated)
        {
            var returnType = method.ReturnType.IsGenericType ? method.ReturnType.GetGenericArguments()[0] : method.ReturnType;

            foreach (var type in EnumerateTypes(returnType, typesEnumerated))
            {
                yield return type;
            }
            foreach (var parameter in method.GetParameters())
            {
                foreach (var type in EnumerateTypes(parameter.ParameterType, typesEnumerated))
                {
                    yield return type;
                }
            }
        }

        static public IEnumerable<MethodInfo> GetControllerActions(this Type controllerType)
        {
            if (!controllerType.IsWebMvcController())
            {
                throw new ArgumentOutOfRangeException($"Type {controllerType.Name} MUST extends System.Web.Mvc.Controller", nameof(controllerType));
            }
            foreach (var method in controllerType.GetMethods())
            {
                var httpVerb = method.FirstOrDefaultHttpMethodAttribute();
                if (httpVerb != null)
                {
                    yield return method;
                }
            }
        }

    }
}
