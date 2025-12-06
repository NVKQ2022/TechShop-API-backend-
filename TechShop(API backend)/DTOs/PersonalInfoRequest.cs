namespace TechShop_API_backend_.DTOs
{
    public class PersonalInfoRequest
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Birthday { get; set; }

        public string Gender { get; set; }

        public string Avatar { get; set; }
    }
}
