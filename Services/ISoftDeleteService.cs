namespace EnglishCenter.API.Services
{
    public interface ISoftDeleteService
    {
        Task<bool> RestoreAsync<TEntity>(
            int id)
            where TEntity : class;
    }
}