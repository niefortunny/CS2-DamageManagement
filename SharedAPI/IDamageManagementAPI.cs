public interface IDamageManagementAPI
{
    delegate bool CallOriginalOnTakeDamageMethod();
    void Hook_OnTakeDamage(CallOriginalOnTakeDamageMethod handler);
    void Unhook_OnTakeDamage(CallOriginalOnTakeDamageMethod handler);
}
