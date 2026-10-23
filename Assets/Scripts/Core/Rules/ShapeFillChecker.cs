using System.Collections.Generic;

namespace ShapeInventory.Core
{
    /// <summary>Result of the "is the object's shape filled" check. More fields can be added later.</summary>
    public sealed class ShapeFillResult
    {
        public bool IsFilled;
    }

    /// <summary>
    /// PLACEHOLDER - to be implemented by the project owner.
    /// Decides whether the items placed in an object inventory fill the object's shape.
    /// </summary>
    public static class ShapeFillChecker
    {
        public static ShapeFillResult IsShapeFilled(IReadOnlyList<PlacedItem> items)
        {
            // TODO(owner): implement real fill logic. For now every object counts as filled.
            return new ShapeFillResult { IsFilled = true };
        }
    }
}
