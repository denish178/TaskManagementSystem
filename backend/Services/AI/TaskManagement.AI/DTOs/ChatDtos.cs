namespace TaskManagement.AI.DTOs;

public record ChatRequest(
    string Message
);

public record ChatResponse(
    string Answer
);