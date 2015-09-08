using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Apex7000_BillValidator;

namespace API_Test
{
    [TestClass]
    public class SlaveCodex_Test
    {

        [TestMethod]
        public void TestToSlaveMessage()
        {
            byte[] testData;

            // Test the we return invalid command on empty data
            testData = new byte[] { };

            Assert.AreEqual(SlaveCodex.SlaveMessage.InvalidCommand, SlaveCodex.ToSlaveMessage(testData));

            // Test the we return invalid command on too large of data
            testData = new byte[12];

            Assert.AreEqual(SlaveCodex.SlaveMessage.InvalidCommand, SlaveCodex.ToSlaveMessage(testData));
        }


        [TestMethod]
        public void TestGetState()
        {

            States state;
            SlaveCodex.SlaveMessage message;
            byte[] testData;

            // Idling and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.Idling, state);

            // Accepting and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x02, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.Accepting, state);

            // Escrowed and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x04, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.Escrowed, state);

            // Stacking and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x8, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.Stacking, state);

            // Returning and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x20, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.Returning, state);

            // Jammed and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x00, 0x14, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.BillJammed, state);

            // Cashbox full and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x00, 0x18, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.StackerFull, state);

            // Acceptor Failure and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x00, 0x10, 0x04, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            state = SlaveCodex.GetState(message);
            Assert.AreEqual(States.AcceptorFailure, state);

            Assert.IsTrue(SlaveCodex.IsCashboxPresent(message));

        }

        [TestMethod]
        public void TestGetEvent()
        {

            Events events;
            SlaveCodex.SlaveMessage message;
            byte[] testData;

            // Idling and Stacked
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x11, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            events = SlaveCodex.GetEvents(message);
            Assert.AreEqual(Events.Stacked, events);

            // Idling and returned
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x41, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            events = SlaveCodex.GetEvents(message);
            Assert.AreEqual(Events.Returned, events);

            // Cheated and returning
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x20, 0x11, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            events = SlaveCodex.GetEvents(message);
            Assert.AreEqual(Events.Cheated, events);

            // Rejected and cashbox present
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x12, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            events = SlaveCodex.GetEvents(message);
            Assert.AreEqual(Events.BillRejected, events);

            // Power up
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x00, 0x00, 0x01, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            events = SlaveCodex.GetEvents(message);
            Assert.AreEqual(Events.PowerUp, events);          
        }


        [TestMethod]
        public void TestGetCredit()
        {
            int credit;
            SlaveCodex.SlaveMessage message;
            byte[] testData;

            // None/Unknown
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x00, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(0, credit);

            // $1
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x08, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(1, credit);

            // $2
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x10, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(2, credit);

            // $5
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x18, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(3, credit);

            // $10
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x20, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(4, credit);

            // $20
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x28, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(5, credit);

            // $50
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x30, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(6, credit);

            // $100
            testData = new byte[] { 0x02, 0x0B, 0x21, 0x01, 0x10, 0x38, 0x00, 0x11, 0x11, 0x03, 0x3B };
            message = SlaveCodex.ToSlaveMessage(testData);

            credit = SlaveCodex.GetCredit(message);
            Assert.AreEqual(7, credit);   
        }

    }
}
