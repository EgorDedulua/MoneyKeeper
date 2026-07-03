namespace MoneyKeeper.Application.Common.Validation
{
    public interface ICategoryOwnershipValidationModel
    {
        int UserId { get; set; }

        int CategoryId { get; set; }
    }
}
