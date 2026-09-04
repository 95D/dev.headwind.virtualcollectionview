namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Placement of the whole content block along the main axis when the content is shorter than the viewport.
    /// </summary>
    /// <remarks>
    /// Has no effect once the content fills the viewport (i.e. once anything
    /// scrolls). Shared by layout managers because its semantics do not depend
    /// on the placement rule.
    /// </remarks>
    public enum MainAlignment
    {
        Start,
        Center,
        End
    }
}
