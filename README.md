# GameFoundation
Game Foundation provides pre-built common game systems that are flexible and extensible so that developers can focus on building unique gameplay.

# Assembly
For game assembly, normally, you will add these

```
"GameFoundation.Script",
"GameFoundation.AssetLibrary",
"GameFoundation.BlueprintFlow",
"GameFoundation.UIModule",
"GameFoundation.Utilities",
"GameFoundation.Models",
"GameFoundation.Network.HttpServices",
```

<img width="295" alt="image" src="https://user-images.githubusercontent.com/9598614/193384245-9ba3a98e-fe63-4921-b05a-8e37967c2413.png">


## Installation

### Requirement

* Unity 2021.3 or later

### Using Package Manager
Firstly, you need to use [GitDependencyResolverForUnity](https://github.com/mob-sakai/GitDependencyResolverForUnity) package by mob-sakai to resolves git-based dependencies by adding https://github.com/mob-sakai/GitDependencyResolverForUnity.git to Package Manager.
Then, you add Game Foundation Core by https://github.com/GameDevelopmentKit/GameFoundation.git

Or find the `manifest.json` file in the `Packages` directory in your project and edit it as follows:
```
{
  "dependencies": {
    "com.coffee.git-dependency-resolver": "https://github.com/mob-sakai/GitDependencyResolverForUnity.git",
    "com.gdk.core": "https://github.com/GameDevelopmentKit/GameFoundation.git",
    ...
  },
}
```

### Using Git submodule
This package need to be cloned to Packages folder. And we still use [GitDependencyResolverForUnity](https://github.com/mob-sakai/GitDependencyResolverForUnity) package like above
```
git submodule add git@github.com:GameDevelopmentKit/GameFoundation.git .\Packages\com.gdk.core
```

<br>
