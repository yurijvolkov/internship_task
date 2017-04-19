using System;

namespace Yandex_Internship.Domain
{
    /// <summary>
    /// Domain model for order
    /// Important : it's not used for any ORM 
    /// </summary>
    class Order
    {
        public Order(int id, DateTime dateTime, int productId, double amount)
        {
            Id = id;
            DateTime = dateTime;
            ProductId = productId;
            Amount = amount;
        }
        
        /// <summary>
        /// Ignored while loading entity to database
        /// </summary>
        internal int Id {  get; }

        internal DateTime DateTime { get; }

        internal int ProductId { get; }

        internal double Amount { get; }
    }
}
