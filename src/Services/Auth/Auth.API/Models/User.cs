using Microsoft.AspNetCore.Identity;

namespace Auth.API.Models
{
    public class User : IdentityUser
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = string.Join(' ',
                    value.Split(' ').Select(n => n[0].ToString().ToUpper() + n.Substring(1).ToLower()).ToArray()
                );
            }
        }

        public Status Status { get; set; }
        public int? ImageId { get; set; }
        public Image Image { get; set; }
    }

    public enum Status
    {
        Default = 1,
        Blocked = 2
    }
}
