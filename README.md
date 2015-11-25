# Nimrod
An  ASP.NET MVC to TypeScript Converter

# Usage

### Command line

Use the Nimrod.Console utilities to generate files
```
Nimrod.Console.exe -m typescript -o .\\src\\ServerApi.Generated --files=..\\assembly1.dll:..\\assembly2.dll',
```
###  Options

|Name|Alias|Description|
|:----|:----|:-----|
|--module|-m|Module mode, valid values are `typescript` for [typescript](tsModule) modules style and `require` for [require](requirejs) modules|
|--output|-o|Directory where files will be generated|
|--files|-f|Assembly files to read, separated by a colon. Example : --files=bin\\Assembly1.dll:bin\\Assembly2.dll|
|--verbose|-v|Prints all messages to standard output|
|--help|-h|Prints all messages to standard output|

# Features

When you launch Nimrod, the following steps are going to happens:

 - Read all the assembly to search for class which inherits from [Controllers](controllers)
 - From thoses classes, search public method which have one of the following attribute [HttpGet], [HttpPost], [HttpPut], [HttpDelete]
 - Search all classes referenced by the generic return of those methodes (see below), and their arguments
 - Generate typescript files for all referenced classes, and for the controllers
 
### Example

C# code
```
public class Movie
{
    public string Name { get; }
    public double Rating { get; }
    public List<string> Actors { get; }
}
public class MovieController : Controller
{
    [HttpGet]
    public JsonNetResult<Movie> Movie(int id)
    {
        throw new NotImplementedException();
    }
}
```
TypeScript code
```
module Nimrod.Test.ModelExamples {
    export interface IMovie {
        Name: string;
        Rating: number;
        Actors: string[];
    }
}
module Nimrod.Test.ModelExamples {
    export interface IMovieService {
        Movie(restApi: RestApi.IRestApi, id: number, config?: RestApi.IRequestShortcutConfig): RestApi.IPromise<Nimrod.Test.ModelExamples.IMovie>;
    }
    export class MovieService implements IMovieService {
        public Movie(restApi: RestApi.IRestApi, id: number, config?: RestApi.IRequestShortcutConfig): RestApi.IPromise<Nimrod.Test.ModelExamples.IMovie> {
            (config || (config = {})).params = {
                id: id,
            };
            return restApi.Get<Nimrod.Test.ModelExamples.IMovie>('/Movie/Movie', config);
        }
    }
    service('serverApi.MovieService', MovieService);
}

```
The interfaces `IRequestShortcutConfig`, `IRestApi` and `IPromise` should be added accordingly to your javascript framework. It could be Angular or React or whatever, here is an example that works for Angular:

```

    export interface IRestApi {
        Delete<T>(url: string, config?: IRequestShortcutConfig): IPromise<T>;
        Get<T>(url: string, config?: IRequestShortcutConfig): IPromise<T>;
        Post<T>(url: string, data: any, config?: IRequestShortcutConfig): IPromise<T>;
        Put<T>(url: string, data: any, config?: IRequestShortcutConfig): IPromise<T>;
    }
    export interface IRequestShortcutConfig extends ng.IRequestShortcutConfig {
    }
    export interface IPromise<T> extends ng.IPromise<T> {
    }
```
The `restApi` parameter is a wrapper you must write that will wrap the logic of the ajax request. Here is an example with Angular:

```
class RestApi implements IRestApi {
        static $inject: string[] = ['$http', '$q'];
        constructor(private $http: ng.IHttpService, private $q: ng.IQService) {
        }

        public Delete<T>(url: string, config?: ng.IRequestShortcutConfig) {
            var deferred = this.$q.defer<T>();
            this.$http.delete<T>(url, config)
                .success(response => { deferred.resolve(response); })
                .error((response, status, headers, config) => deferred.reject(response));
            return deferred.promise;
        }
        etc...
}
```
---
# Questions & Answers

**Q: Why the generator didn't write the class JSONNetResult?**

On the C# side, the return type of a controller method is always wrapped inside a `Json` something stuff. As we need to detect the real type of the return, we oblige the user of our library to wrap the return type into a generic. And so we don't generate the model itself, but the type inside of it. So if the return type is `Foo<Bar>`, we are going to generate only `Bar`, not `Foo`.

**Q: I only need those models generators, not all the fancy service controller whatever stuff, can you do it?**

We plan to deliver a version of the converter only for POCOs, but pull requests are welcomed


# Todos

 - Refactoring
 - Docs
 - Limitations on generics, specifically return embed generics. IE : a method returning a `Json<List<Tuple<int, string>>>` is not going to work

   [tsModule]: <http://www.johnpapa.net/typescriptpost4>
   [requirejs]: <http://requirejs.org/>
   [controllers]: <https://msdn.microsoft.com/library/system.web.mvc.controller>
   [HttpGet]: <https://msdn.microsoft.com/library/system.web.mvc.httpgetattribute.aspx>
   [HttpPost]: <https://msdn.microsoft.com/library/system.web.mvc.httppostattribute.aspx>
   [HttpPut]: <https://msdn.microsoft.com/library/system.web.mvc.httpputattribute.aspx>
   [HttpDelete]: <https://msdn.microsoft.com/library/system.web.mvc.httpdeleteattribute.aspx>


