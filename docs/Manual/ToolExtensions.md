# Tool Extensions {#ToolExtensions}

## Tool Composition

Whenever the Nuke Tooling API gives you a Tool delegate it is a clean slate, meaning you need to provide it your arguments, environment variables, how one reacts to its output etc. With the intended usage once these parameters are given to the `Tool` delegate it immediately executes the tool it represents.

However there are cases when multiple tasks with one tool requires a common set of arguments, environment variables or any other parameters `Tool` accepts. In such cases the API preferably would still provide a `Tool` delegate but the user of that API shouldn't need to repeat the boilerplate setup for that task involving the tool. The solution Nuke.Cola provides is the `With` extension method which allows to combine together the parameters `Tool` accepts but in multiple steps. See:

```CSharp
public static Tool MyTool => ToolResolver.GetPathTool("my-tool");
public static Tool MyToolMode => MyTool.With(arguments: "my-mode");
public static Tool WithFoo(this Tool tool) => tool.WithEnvVar("FOO", "bar");
public static Tool WithBins(this Tool tool) => tool.WithPathVar(NukeBuild.TemporaryDirectory / "tempBins");

// ...

MyTool("args"); // use normally
MyToolMode("--arg value"); // yields `my-tool my-mode --arg value`
MyToolMode
    .WithFoo()
    .WithBins()
    .WithSemanticLogging()("--arg value"); // excercise for the reader
```

## `ToolEx`

A reimplementation of Nuke's `Tool` with extended features. All the tool composition features are also available for `ToolEx`.

### Standard Input / Piping

Feed data into a tool via standard input:

```CSharp
ToolExResolver.GetPathTool("myTool")(input: s => s.WriteLine("Hello"));
```

The `Pipe` extension method is using this feature and 'tool composition' to allow exchange between processes comfortably, like in almost all shell environments.

```CSharp
var toolA = ToolExResolver.GetPathTool("toolA");
var toolB = ToolExResolver.GetPathTool("toolB");
var toolC = ToolExResolver.GetPathTool("toolC");

toolA("-foo bar")!
    .Pipe(toolB)("-arg 1")!
    .Pipe(toolC)("-log debug");
```

We can also easily queue up inputs via extension methods. When combining tool arguments, standard-input delegates are combined with the `+` operator which is basically queuing them one after the other.

```CSharp
// A fictitious tool which just repeats back everything until stream is closed
var parrot = ToolExResolver.GetPathTool("parrot");

parrot
    .WithInput("I'm Polly")        // Queue a single line
    .WithInput(["Hello", "World"]) // Queue multiple lines from collection of strings
    .CloseInput()                  // Polly is polite and extremely patient and they will wait for us until we indicate that we're finished
    ()!                            // Execute with no arguments
    .Pipe(parrot)()                // Repeat again.
```

Output (without additional Nuke logging):

```
I'm Polly
Hello
World
I'm Polly
Hello
World
```

Note that unlike other 'input' extension methods, `Pipe` closes the stream by default. So when used with many tools, `ClosePipe()` or `close: true` are not needed to be repeated all the time. If you want to manipulate standard input after the results of `Pipe` add `close: false` to its arguments.