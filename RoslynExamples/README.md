# Welcome

## General Structure

The samples are split into three folders depending on their level of complexity. The Introduction folder has examples that show some basic setups and graphs. The Advanced examples have more elaborated graphs and animation concepts. The StressTests folder has scenes that test the limits of some of the animation core functions.

## Input

Some samples are interactive and use the horizontal and/or vertical inputs to modify the Entities data.

To do this, a GameObject with a script inheriting from the `AnimationInputBase` is put in the Scene (not the SubScene). This object keeps a list of Entities and sets their `ISampleData` Component value on the main thread in the `Update` function. The overriden function `UpdateComponentData` computes the new data used for the Entities Components, and you could use other inputs than Horizontal and Vertical axis if you want.

## Bone Rendering

To use the bone rendering, the RigPrefab must have the script `BoneRendererComponent` attached to it. The `BoneRendererConversion` System will convert it. In addition to the rig Entity, the `BoneRendererEntityBuilder` will create two Entities: one with the data required to compute the world matrices of the bones, that will have a reference to the rig Entity, a buffer for the bones' world matrices and a size for the bones; the other entity will have components for the instanced rendering, that is the color and shape of the bones, and a reference to the bone renderer data entity.

In the `BoneRendererSystemGroup`, two systems will compute the matrices then and render the bones, each working on the Entities previsouly mentioned:
1) The `BoneRendererMatrixSystem` is a `JobComponentSystem` and as such is executed on multiple threads. This System computes the bones's matrices using the position of the rig's joints and the scale of the bones. It then sets the value of the `BoneWorldMatrix` Component;
2) The `BoneRendererRenderingSystem` is updated after the `BoneRendererMatrixSystem` System and is a `ComponentSystem`. It's necessary to work on the main thread here because the bones are drawn using `UnityEngine.Graphics.DrawMeshInstanced`. The buffer of `BoneWorldMatrix`, computed in the previous system, is memcopied into a `Matrix4x4` array that is then used for GPU instanciation.

Note that currently the bone renderer uses a custom shader to render the bones, Runtime/BoneRenderer/Shaders/BoneRenderer.shader.

# Samples
