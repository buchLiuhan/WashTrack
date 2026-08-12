// Messages/CustomerCreatedMessage.cs
using CommunityToolkit.Mvvm.Messaging.Messages;
using WashTrack.Models;

namespace WashTrack.Messages
{
    public class CustomerCreatedMessage : ValueChangedMessage<Customer>
    {
        public CustomerCreatedMessage(Customer customer) : base(customer) { }
    }
}