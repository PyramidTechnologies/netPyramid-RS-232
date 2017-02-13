Pause/Resume + Escrow Mode Example
==================================
If your application requires you to have near-instant control over enabling and disabling the bill acceptor,
you should be using the Pause() API. This allows you to immediately* stop or start accepting note without 
breaking comms with the bill acceptor.

*immediately: The response time is dependent on the poll rate and precisely where in the message loop you issue 
the command. Worst case, the respose time will be equal to your poll rate. The default poll rate is 50 ms.

These command work in either Escrow or Non-Escrow mode.


##Highlights

1. Subscribe

    validator.OnEscrow +=validator_OnEscrow;
	
2. Control your state
    
	AppState = InternalState.Escrow;
	
3. Execute your action

      // If Escrowed, prompt for stack or reject
      else if (AppState == InternalState.Escrow)
      {
	      validator.Stack();	  
	  }
	  
	  
4. Emit the PauseAcceptance() or ResmeAcceptance() when the acceptor is in the Idle state. If you call PauseAcceptance()
while stacking, the note may return immediately depending on your poll rate. It is advised that either check the state 
validator.PreviousState or maintain your own state if you have more complex customer requirements. The PreviousState 
property is called such because it is not realtime. It is the state reported by the slave during the previous message loop.

