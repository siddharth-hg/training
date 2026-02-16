import asyncio
from autogen_agentchat.agents import AssistantAgent, CodeExecutorAgent,UserProxyAgent
from autogen_ext.models.openai import OpenAIChatCompletionClient
from pathlib import Path
from autogen_ext.code_executors.local import LocalCommandLineCodeExecutor
from autogen_agentchat.conditions import TextMentionTermination
from autogen_agentchat.teams import RoundRobinGroupChat
from autogen_agentchat.ui import Console
from streamlit import user

async def main():
    model_client = OpenAIChatCompletionClient(
        model="gemini-3-flash",
        api_key="AIzaSyBxeN-hh66K1hmwHEo97DRr1Ii0qSD6Dfg",
        base_url="https://generativelanguage.googleapis.com/v1beta/openai/",
        model_info={
            "vision": True,
            "function_calling": True,
            "json_output": True,
            "family": "gemini-2.0-flash-exp",
            "structured_output": True,
        }
)

    coder = AssistantAgent(
        "coder",
        model_client=model_client,
        system_message=(
            "You are a senior engineer. Think step-by-step, then output ONLY runnable "
            "Python inside ```pythonthon``` blocks—no commentary."
        ),
    )

    executor = CodeExecutorAgent(
        "executor",
        model_client=model_client,  # lets it narrate results
        code_executor=LocalCommandLineCodeExecutor(work_dir=Path.cwd() / "runs"),
)

    user = UserProxyAgent("user")

    termination = TextMentionTermination("exit", sources=["user"])
    team = RoundRobinGroupChat(
        [user, coder, executor], termination_condition=termination,max_turns=10
    )

    try:
        await Console(
        team.run_stream()
    )
    finally:
        await model_client.close()
    
asyncio.run(main())