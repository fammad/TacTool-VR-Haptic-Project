# Implementation Plan - Material Interactions (Haptics, Audio, and Deformation)

## Project Overview
- **Objective**: Implement material-specific reactions for tools (Hammer, Saw, File) hitting materials (Wood, Metal).
- **Key Features**:
  - Haptic feedback patterns for different interactions.
  - Audio feedback for impacts and continuous actions (sawing/filing).
  - Physical/Visual deformation:
    - Hammer on Wood: Slight deformation.
    - Hammer on Metal: Bending of the metal rod (bottom fixed).
    - Saw on Wood / File on Metal: "Scratch" effects.

## Game Mechanics
- **Interaction Logic**:
  - Tools detect collisions with materials using `ToolCollisionDetector`.
  - Collision strength determines haptic intensity and sound volume.
  - Impact direction determines the direction of bending.

## Key Asset & Context
- **Scripts**:
  - `MaterialInteractionHandler.cs`: Central script to handle haptics, audio, and routing to deformation logic.
  - `DeformableMaterial.cs`: Handles vertex-based bending for the metal rod and wood.
- **Components**:
  - `AudioSource`: Added to the VR player or materials to play sounds.
  - `XRGrabInteractable`: Used to identify which controller is holding the tool for haptics.

## Implementation Steps

### 1. Update Tool Setup
- **File**: `Assets/Scripts/ToolCollisionDetector.cs`
- **Change**: 
  - Add `Saw` to `ToolType` enum.
  - Ensure all tools have the script and correct `ToolType` assigned.
  - Add `onCollisionStay` logic for Saw and File to handle continuous interactions (Haptics/Audio during sawing).

### 2. Implement Deformable Material Script
- **File**: `Assets/Scripts/DeformableMaterial.cs`
- **Logic**:
  - Store original vertices.
  - `ApplyDeformation(Vector3 localPoint, Vector3 localForce)`:
    - For Metal: Bend the mesh by offsetting vertices based on distance to bottom (height-weighted).
    - For Wood: Small localized indentation.
- **Attachment**: 
  - Attach to `Metal - Bendable` and `Log`.

### 3. Implement Material Interaction Handler
- **File**: `Assets/Scripts/MaterialInteractionHandler.cs`
- **Logic**:
  - Subscribe to `ToolCollisionDetector.onValidHit`.
  - **Audio**: Play specific clips based on Tool + Material combo.
  - **Haptics**: Use `XRGrabInteractable.interactorsSelecting` to find the active controller and call `SendHapticImpulse`.
  - **Bending**: Call `DeformableMaterial.ApplyDeformation`.

### 4. Scene Configuration
- Add `AudioSource` to a global "AudioManager" or the XR Origin.
- Assign Audio Clips (Metal hit, Wood hit, Sawing, Filing).
- Tags: Ensure `Log` is tagged "Wood" and `Metal - Bendable` is tagged "Metal".

## Verification & Testing
- **Manual Test**:
  - Pick up the Hammer, hit the Metal Rod. Check if it bends and vibrates.
  - Pick up the Saw, touch the Wood. Check for continuous sound/vibration and "scratch" (if implemented).
- **Edge Cases**:
  - Hitting with very low velocity (should not trigger).
  - Dropping the tool (should not trigger haptics if not held).
