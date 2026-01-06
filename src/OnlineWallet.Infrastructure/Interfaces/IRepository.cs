using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure.Interfaces
{
    /// <summary>
    /// Generic repository interface for data access operations.
    /// Defines common CRUD operations for all most entities e.g. (UsersRepository).
    /// </summary>
    /// <typeparam name="T">Type of entity this repository handles</typeparam>
    public interface  IRepository<T>
    {
        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task AddAsync(T entity);

        /// <summary>
        /// Retrieves all entities from the repository.
        /// </summary>
        /// <returns>All entities in the repository</returns>
        public Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Retrieves an entity by its unique identifier.
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <returns>Entity if found</returns>
        public Task<T?> GetByIdAsync(Guid id);


        /// <summary>
        /// Updates an existing entity in the repository.
        /// </summary>
        /// <param name="entity">Entity to update</param>
        public void  Update(T entity);

        /// <summary>
        /// Removes an entity from the repository.
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        public void Delete(T entity);

    }
}
