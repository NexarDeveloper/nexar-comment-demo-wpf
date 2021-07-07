# Nexar.Comment Demo

[nexar.com]: https://nexar.com/

Demo Altium 365 comment browser powered by Nexar.

**Projects:**

- `Nexar.Comment` - WPF application, comment browser
- `Nexar.Client` - GraphQL StrawberryShake client

## Prerequisites

Visual Studio 2019.

You need your Altium Live credentials and have to be a member of at least one Altium 365 workspace.

In addition, you need an application at [nexar.com] with the Design scope.
Use the application client ID and secret and set environment variables `NEXAR_CLIENT_ID` and `NEXAR_CLIENT_SECRET`.

## How to use

Open the solution in Visual Studio.
Ensure `Nexar.Comment` is the startup project, build, and run.

If you run with the debugger then it may break due to "cannot read settings".
Ignore and continue (<kbd>F5</kbd>). The settings are stored on exiting.
Next runs should not have this issue.

The identity server sign in page appears. Enter your credentials and click `Sign In`.

The application window appears with the left tree panel populated with your workspaces.

Expand workspaces to their projects, expand projects to their comment threads, select a thread.
As a result, the thread comments are shown in the right top panel, scrolled to the end.

## Operations

- **Open snapshot**

    The first comment usually contains the link "Original snapshot".
    Click to open the snapshot image in a new window.

- **Send comment**

    Type the new comment text in the right bottom panel and send it by
    <kbd>Ctrl+Enter</kbd>.

- **Open project**

    Press <kbd>F2</kbd> to open Altium 365 with the project or workspace
    currently selected in the tree.

- **Refresh comments**

    Press <kbd>F5</kbd> to fetch the current thread comments.

- **Delete comment**

    Click the comment header and press <kbd>Delete</kbd> in order to delete the
    comment, with the confirmation dialog. The comment must be created by the
    signed user.

## Building blocks

The app is built using Windows Presentation Foundation, .NET Framework 4.7.2.

The data are provided by Nexar API: <https://api.nexar.com/graphql>.
This is the GraphQL endpoint and also the Banana Cake Pop GraphQL IDE in browsers.

The [HotChocolate StrawberryShake](https://github.com/ChilliCream/hotchocolate) package
is used for generating strongly typed C# client code for invoking GraphQL queries.
Note that StrawberryShake generated code must be compiled as netstandard.
That is why it is in the separate project `Nexar.Client` (netstandard).
The main project `Nexar.Comment` (net472) references `Nexar.Client`.
