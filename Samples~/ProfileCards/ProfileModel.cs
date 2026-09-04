namespace Headwind.VirtualCollectionView.Samples
{
    public sealed class ProfileModel
    {
        public ProfileModel(int id, string name, string title, float hue)
        {
            Id = id;
            Name = name;
            Title = title;
            Hue = hue;
        }

        public int Id { get; }
        public string Name { get; }
        public string Title { get; }

        /// <summary>Avatar color hue in [0, 1).</summary>
        public float Hue { get; }

        public string Initial => Name.Substring(0, 1);
    }
}
