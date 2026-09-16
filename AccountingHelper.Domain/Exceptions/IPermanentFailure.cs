namespace AccountingHelper.Domain.Exceptions;

// Marks an error that retrying cannot fix: the input is wrong, the row is absent, the rule is
// broken. Retrying only delays the inevitable trip to the error queue. The marker lives in
// Domain because the transport must decide whether to retry without seeing Application. 
public interface IPermanentFailure
{
    
}