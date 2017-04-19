namespace Yandex_Internship.Domain
{
    /// <summary>
    /// Domain model for product
    /// Important : it's not used for any ORM 
    /// </summary>
    class Product
    {
        public Product(int id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Ignored while loading entity to database
        /// </summary>
        internal int Id { get; }

        internal string Name { get; }
    }
}
