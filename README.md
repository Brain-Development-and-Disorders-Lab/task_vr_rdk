# VR Random Dot Kinematogram (RDK) Task

> This task is a VR-compatible adaptation of an existing [RDK task](https://github.com/Brain-Development-and-Disorders-Lab/task_rdk).

## Getting Started

This repository contains two versions of the task, one browser-based (WebXR) implementation and an implementation using Unity. The WebXR implementation is archived at version 1.0.3, and was implemented using the [Ouvrai](https://github.com/EvanCesanek/Ouvrai) software package. The primary advantage of migrating to Unity is the ability to use eye-tracking capabilities and operate natively on the Meta Quest Pro headsets. [Unity Experiment Framework](https://immersivecognition.com/unity-experiment-framework/) is used to create experiment structure and enable data collection. The [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) package is used to install the [Math.NET Numerics](https://numerics.mathdotnet.com/) package.

## Remote Monitoring

To monitor and control the VR experiment remotely, the `Headsup` control panel was developed. Using a Python GUI, the control panel can remotely launch the experiment (ADB over Wi-Fi required), start the experiment, view experiment state, and receive live logging output wirelessly from the headset.

Headsup can be found on [GitHub](https://github.com/Brain-Development-and-Disorders-Lab/headsup). The repository includes a `.cs` file, `HeadsupServer.cs`, that can be integrated with other VR experiments to allow for remote monitoring and control.

## License

<!-- CC BY-NC-SA 4.0 License -->
<a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">
  <img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" />
</a>
<br />
This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.

## Issues and Feedback

Please contact **Henry Burgess** <[henry.burgess@wustl.edu](mailto:henry.burgess@wustl.edu)> for all code-related issues and feedback.
