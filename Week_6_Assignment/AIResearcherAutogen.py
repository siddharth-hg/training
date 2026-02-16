import os
from dotenv import load_dotenv
from crewai import Agent, Task, Crew, LLM
from crewai.tools import tool
from crewai_tools import PDFSearchTool
 
# 1) Load environment variables
load_dotenv()

# CRITICAL: Set these BEFORE importing/using PDFSearchTool
os.environ["OTEL_SDK_DISABLED"] = "true"  # Disable telemetry
os.environ["OPENAI_API_KEY"] = "sk-dummy-key-not-used"  # Required by PDFSearchTool validation
 
# 2) Build a Gemini LLM for CrewAI
#    Use the `gemini/` prefix so CrewAI routes via LiteLLM's Gemini (AI Studio) provider
#    and authenticates with GEMINI_API_KEY.
gemini_llm = LLM(
    model="gemini/gemini-2.5-flash",
    api_key=os.environ["GEMINI_API_KEY"],
    temperature=0.2,
    # For Gemini you can use either max_tokens or max_completion_tokens; LiteLLM handles both.
    max_tokens=800
)
 
# 3) Define Agents (now powered by Gemini)
researcher = Agent(
    role="AI Researcher",
    goal="Find recent breakthroughs in AI.",
    backstory="An expert in AI keeping up with the latest research.",
    verbose=True,
    llm=gemini_llm
)
 
writer = Agent(
    role="Technical Writer",
    goal="Write a short blog post from research data.",
    backstory="A skilled writer who can turn complex info into engaging posts.",
    verbose=True,
    llm=gemini_llm
)
 
# 4) Define Tasks (optionally pass llm here too, but it's redundant since agents already have llm)
research_task = Task(
    description="Find the 3 most important AI research breakthroughs of 2024. Include a link for each.",
    expected_output=(
        "- Breakthrough 1: <name> — 1–2 lines. Source: <URL>\n"
        "- Breakthrough 2: <name> — 1–2 lines. Source: <URL>\n"
        "- Breakthrough 3: <name> — 1–2 lines. Source: <URL>"
    ),
    agent=researcher,
    verbose=True
)
 
write_task = Task(
    description="Write a concise, 300-word blog post summarizing the 3 breakthroughs for a general tech audience.",
    expected_output="~300‑word blog post with a title and short intro, using the research findings.",
    agent=writer,
    context=[research_task],  # The writer uses the output of the research_task
    verbose=True
)
 
# 5) Define and run the Crew
ai_blog_crew = Crew(
    agents=[researcher, writer],
    tasks=[research_task, write_task],
    verbose=True
    # You could also add llm=gemini_llm here, but it's not required since each agent already has it.
)
 
# result = ai_blog_crew.kickoff()
# print("\n=== FINAL OUTPUT ===\n")
# print(result)

# Define a custom tool
@tool("search_google")
def google_search(query: str) -> str:
    """Searches Google and returns a simulated answer."""
    # In a real scenario, this would integrate with an actual search API
    return f"Simulated search result for: {query}"

# Add the tool to an agent
researcher = Agent(
    role="AI Researcher",
    goal="Find the latest trends in generative AI.",
    backstory="You use external tools to get real-time information.",
    tools=[google_search],
    verbose=True,
    llm=gemini_llm
)

# Initialize the tool for a specific PDF
pdf_tool = PDFSearchTool(pdf="D:/Training/financial_report_15_pages.pdf",llm=gemini_llm)

# Create an agent with the tool
analyst = Agent(
    role="Financial Analyst",
    goal="Extract revenue trends from the report.",
    backstory="An expert at analyzing financial documents.",
    tools=[pdf_tool],
    verbose=True,
    llm=gemini_llm
)

# Create a task that prompts the agent to use the tool
task = Task(
    description="Read the attached PDF and summarize the revenue trends.",
    expected_output="A short summary of revenue numbers and patterns.",
    agent=analyst
)

# Run PDF Analysis Crew
print("\n\n=== STARTING PDF ANALYSIS ===\n")
pdf_crew = Crew(
    agents=[analyst],
    tasks=[task],
    verbose=True
)
pdf_result = pdf_crew.kickoff()
print("\n=== PDF ANALYSIS OUTPUT ===\n")
print(pdf_result)