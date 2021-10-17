using System.Runtime.Serialization;

namespace az_func.Common.EventReceivers
{
    [DataContract(Name = "ProcessEventResponse", Namespace = "http://schemas.microsoft.com/sharepoint/remoteapp/")]
    public class ProcessEventResponse
    {
        [DataMember]
        public SPRemoteEventResult ProcessEventResult { get; set; }
    }
}
