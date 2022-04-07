# SoLoader
SoLoader is a native code loader for Android. It takes care of unpacking your native libraries
and recursively loads dependencies on platforms that don't support that out of the box.

## Requirements
SoLoader can be included in any Android application.

## Including SoLoader in your apps
You can use [prebuilt aars](https://github.com/facebook/soloader/releases/latest)
or fetch SoLoader from Maven repository by adding the following to your
`build.gradle` file:
```groovy
compile 'com.facebook.soloader:soloader:0.1.0+'
```
to your build.gradle file.

## Building from source
To build SoLoader from source you'll need [Buck](https://buckbuild.com/).
Onve you have Buck installed execute following commands from the project root
directory:
```shell
  python scripts/fetch_deps.py
  buck build :soloader
```
The build command generates `buck-out/gen/soloader.aar` file.

## Join our community
Please use our [issues page](https://github.com/facebook/soloader/issues) to let us know of any problems.
See the [CONTRIBUTING](https://github.com/facebook/soloader/blob/master/CONTRIBUTING.md) file for how to
help out.

## License
SoLoader is [BSD-licensed](https://github.com/facebook/soloader/blob/master/LICENSE).
We also provide an additional [patent grant](https://github.com/facebook/soloader/blob/master/PATENTS).
