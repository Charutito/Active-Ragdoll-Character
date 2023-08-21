# Unity Active Ragdoll Character

![Unity Version](https://img.shields.io/badge/Unity-2021.3.27f1%20or%20later-green)
![License](https://img.shields.io/badge/License-MIT-blue)

This Unity project provides a solution for creating active ragdoll character. It includes a tool that allows you to apply an active ragdoll system to other rigged characters easily. The active ragdoll system gives your characters more realistic physics-based movement and interactions. 

![image](https://github.com/matieme/Active-Ragdoll-Character/assets/14026025/6555000a-0323-4ba8-9594-b7f6e3cb9e5f)


## Features

- Active ragdoll system for realistic character physics
- Tool for easily applying the active ragdoll system to rigged characters
- Sample character with active ragdoll setup included

## Requirements

- Unity 2021.3.27f1 or later
- Rigged character with animations
- If you don't have an animated character you can make or download one and rig it with Mixamo, this tool has an autocomplete to make it even easier if the rig is exported from Mixamo

## Usage

### Applying Active Ragdoll System

1. Import your rigged character model into the Unity project.
2. Drag the ActiveRagdoll_Player prefab to the scene.
3. Open the Active Ragdoll Binder tool wich is located under the Tool tab in Unity.
4. Drag your Model to the correct parameter.
5. If you are using a Mixamo model you can press the Autocomplete button and the tool will complete the Bone fields.
6. If you are using your own rig you need to match the bones of your rig with the ones on the fields.
7. Finally press the Bind button.
8. Done! you are ready to test your character.

### Adjusting Active Ragdoll Parameters

- **Forward Is Camera Direction**: Determines whether the character's forward movement direction is aligned with the camera's forward direction. If enabled, the character will move relative to the camera's view.
- **Move Speed**: Controls the speed at which the character moves in the intended direction.
- **Turn Speed**: Defines how quickly the character can rotate or turn around.
- **Jump Force**: Specifies the force applied to the character's body to initiate a jump.
- **Auto Get Up When Possible**: If enabled, the character will automatically attempt to get back up into a standing position when conditions allow.
- **Step Prediction**: This parameter is related to predicting the character's step based on their current state.
- **Balance Height**: Represents the desired height at which the character attempts to maintain balance.
- **Balance Strenght**: Determines the strength with which the character tries to maintain balance. This is apllied to the Configurable Joint Angular Drive.
- **Core Strenght**: Defines the strength of the character's core muscles in the context of ragdoll physics. This is apllied to the Configurable Joint Angular Drive.
- **Limb Strenght**: Determines the strength of the character's limb muscles in the context of ragdoll physics.
- **Step Duration**: Specifies how long each step or movement action takes.
- **Step Height**: Controls the height of the character's steps or movement actions.
- **Feet Mount Force**: This represent the force applied to the character's feet for attaching or mounting onto surfaces.
- **Reach Sensitivity**: Relates to how sensitive the character's reach or interaction with objects is.
- **Arm Reach Stiffness**: Defines the stiffness of the character's arms when reaching or interacting with objects.
- **Can Be Knockout by Impact**: If enabled, the character can be knocked out by strong impacts or collisions.
- **Required Force To Be KO**: Specifies the amount of force needed to knock out the character if the "Can Be Knockout by Impact" parameter is enabled.
- **Can Punch**: Determines whether the character is capable of performing punching actions.
- **Punch Force**: Defines the force applied to the target when the character performs a punch.
     

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [Unity Technologies](https://unity.com/) for providing the Unity game engine.
- [Moe Baker's Serialized Dictionary](https://gist.github.com/Moe-Baker/e36610361012d586b1393994febeb5d2) I used this dictionary to serialize the bones dictionary in Unity Inspector

## Contributing

Contributions are welcome! If you have any improvements or bug fixes, feel free to submit a pull request.

## Contact

If you have any questions or suggestions, please feel free to contact me at [mf.milewski@gmail.com](mailto:mf.milewski@gmail.com).

## Special Thanks

[@eruandou](https://github.com/eruandou) - for code cleaning and help!


