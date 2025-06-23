using BloodDonation.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BloodDonation.Repositories.Interfaces
{
    public interface IDonorRepository
    {
        Task<IEnumerable<Donor>> GetAllAsync();
        Task<Donor> GetByIdAsync(int id);
        Task AddAsync(Donor donor);
        Task UpdateAsync(Donor donor);
        Task DeleteAsync(int id);
    }
}
