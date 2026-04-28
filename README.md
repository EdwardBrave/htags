# EB Hierarchical Tags

![GitHub package.json version](https://img.shields.io/github/package-json/v/EdwardBrave/htags)
![Unity 2022.3+](https://img.shields.io/badge/unity-2022.3+-blue.svg)
![GitHub License](https://img.shields.io/github/license/EdwardBrave/htags)

A robust hierarchical tag system for Unity that offers fast comparisons, an efficient event bus, and strongly-typed code generation. Ideal for complex game logic requiring categorized identifiers with inheritance support.

## Key Features

- **Hierarchical Structure**: Organize tags in a tree-like parent-child relationship (e.g., `Damage.Fire`, `Damage.Water.Ice`).
- **Fast Comparisons**: Optimized tag checks to determine if a tag belongs to a parent category or matches exactly.
- **Strongly-Typed Tags**: Automated C# code generation provides type-safe tag references.
- **Hierarchical Event Bus**: Subscribe to a parent tag and receive events triggered by any of its children.
- **DOTS Compatible**: Uses `NativeArray` and blittable structs, making it suitable for high-performance ECS workflows.
- **Advanced Inspector**: Intuitive dropdown-based tag selection with search and hierarchy visualization.

## Installation

1. Open the Unity Package Manager (`Window > Package Manager`).
2. Click the `+` icon and select `Add package from git URL...`.
3. Enter the repository URL.
    - For the latest version:
      `https://github.com/EdwardBrave/htags.git`.
    - For a specific version (replace `v*.*.*` with the desired version):
      `https://github.com/EdwardBrave/htags.git#v*.*.*`

## Core Concepts

### HTagAsset
The central registry where you define and organize your tag hierarchy. It serves as the source for code generation and manages the underlying tag assets.

### HTag Struct
A generated, lightweight struct representing a specific tag. It contains hierarchy information (all parent IDs), allowing for extremely fast relationship checks.

### HTagSet Struct
A collection of tags optimized for bulk operations. It can efficiently check if it contains a specific tag or any tag that descends from a certain category.

### HTag Event Bus
A static event system (`HTagEventBus`) that respects hierarchy. If you raise an event for `A.B.C`, listeners for `A`, `A.B`, and `A.B.C` will all be notified in order (from parent to child).

## Getting Started

### 1. Define Your Tags
1. Create a new `HTagAsset`: `Right Click in Project > Create > HTagAsset`.
2. In the Inspector, add your tags using the **Hierarchical tags list**. Use dots to define nesting (e.g., `Status.Stun`, `Status.Poison`).
3. The system will automatically create individual tag assets in a subfolder (e.g., `MyTags_Tags/`).

### 2. Generate Code
1. In the `HTagAsset` inspector, go to the **Code Generation** section.
2. Specify a **Namespace**, a **Class Name** (e.g., `GameTag`), and a **Folder Path**.
3. Click **Generate Wrapper Code**. This will create the necessary C# files (e.g., `GameTagField.cs`).

### 3. Use in Scripts
Use the generated `Field` classes to expose tag selection in the Inspector.

```csharp
using UnityEngine;
using MyGame.Tags; // Namespace you defined
using HTags.EventBus;

public class DamageReceiver : MonoBehaviour
{
    [SerializeField] private GameTagField resistanceType;

    private void OnEnable()
    {
        // Subscribe to events for this specific tag or its parents
        HTagEventBus.AddListener(resistanceType.Tag, OnResistedEffect);
    }

    private void OnDisable()
    {
        HTagEventBus.RemoveListener(resistanceType.Tag, OnResistedEffect);
    }

    private void OnResistedEffect()
    {
        Debug.Log($"Resisted {resistanceType.Tag} damage!");
    }
}
```

### 4. Raising Events
```csharp
// Raising an event for 'Status.Poison' will also trigger 'Status' listeners
HTagEventBus.Raise(GameTag.Status_Poison);
```

## Technical Details

- **Minimum Unity Version**: 2022.3
- **Dependencies**: None.
- **Memory Management**: The generated `HTag` and `HTagSet` structs use `NativeArray` with `Allocator.Persistent`. 
  - **Static Tags**: The tags generated in the wrapper class (e.g., `GameTag.MyTag`) are static and managed for the lifetime of the application.
  - **Manual Creation**: If you create tags or sets manually via constructors, ensure you call `.Dispose()` to avoid memory leaks.

## License

Author: **Edward Brave**