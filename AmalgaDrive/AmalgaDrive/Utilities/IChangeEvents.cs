namespace AmalgaDrive.Utilities
{
    public interface IChangeEvents
    {
        bool RaiseOnPropertyChanging { get; set; }
        bool RaiseOnPropertyChanged { get; set; }
        bool RaiseOnErrorsChanged { get; set; }
    }
}
