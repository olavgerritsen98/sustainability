namespace GenAiIncubator.LlmUtils.Tests
{
    /// <summary>
    /// Contains meaningful customer-agent phone conversation examples.
    /// </summary>
    internal static class TestContent
    {
        public static readonly string ComplaintConversation = @"
            Agent: Good afternoon, this is EnergyPro Customer Support. How can I assist you today? 
            Customer: I’m really frustrated! My bill this month is twice as high as usual, and I don’t understand why. 
            Agent: I’m sorry to hear that. Let me look into this for you. Can you confirm your account number for me? 
            Customer: Yes, it’s 12345678. 
            Agent: Thank you. I see here that there was an adjustment due to estimated readings. We didn’t receive an actual meter reading for the last two months. 
            Customer: But that’s not my fault! 
            Agent: I completely understand your frustration. What I can do is request an actual reading and have the bill adjusted accordingly. 
            Customer: That would be great, thank you. 
            Agent: I’ve scheduled the reading for this week, and you’ll receive an updated bill shortly after. I apologize for the inconvenience. 
            Customer: Thanks. I appreciate your help.";

        public static readonly string PositiveFeedbackConversation = @"
            Agent: Good morning, this is EnergyPro Customer Support. How can I assist you? 
            Customer: Actually, I just wanted to say thank you. I recently had solar panels installed through your program, and it’s been amazing. 
            Agent: That’s wonderful to hear! We’re thrilled that you’re enjoying the benefits. 
            Customer: Yes, my energy bill has dropped significantly, and the installation team was so professional. 
            Agent: Thank you for the feedback. It means a lot to us. Is there anything else I can assist you with? 
            Customer: No, I just wanted to share my experience. 
            Agent: We really appreciate it. Have a great day!";

        public static readonly string IssueResolutionConversation = @"
            Agent: Hello, this is EnergyPro Customer Support. How can I assist you? 
            Customer: Hi, I seem to have an issue with my account login. I can’t access my online portal. 
            Agent: I’m sorry to hear that. Let’s get this resolved for you. Can you tell me what error message you’re seeing? 
            Customer: It says my password is incorrect, but I’m sure it’s right. 
            Agent: Let’s try resetting your password. I’ll send a reset link to your email. What’s the address associated with your account? 
            Customer: It’s john.doe@example.com. 
            Agent: Thank you. I’ve just sent the reset link. Can you check your email and follow the instructions? 
            Customer: Got it. One moment... Okay, I’ve reset my password and can log in now. 
            Agent: Excellent! Is there anything else I can assist you with? 
            Customer: No, that’s all. Thanks for your help. 
            Agent: You’re welcome! Have a great day.";
    }
}
