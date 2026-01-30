# app.py
import os
import streamlit as st
from langchain_huggingface import HuggingFacePipeline
from langchain_core.prompts import PromptTemplate
from langchain_core.output_parsers import StrOutputParser
from transformers import pipeline, AutoTokenizer

# -----------------------------
# Streamlit Page Config
# -----------------------------
st.set_page_config(
    page_title="Interview Question Generator (LangChain + TinyLlama)",
    page_icon="🤖",
    layout="centered",
)

st.title("🤖 Interview Question Generator")
st.caption("LangChain + TinyLlama on Streamlit")

# -----------------------------
# Sidebar Controls
# -----------------------------
with st.sidebar:
    st.header("⚙️ Settings")
    model_id = st.text_input(
        "Model ID",
        value="TinyLlama/TinyLlama-1.1B-Chat-v1.0",
        help="Hugging Face model repo (causal LM).",
    )
    temperature = st.slider("Temperature", min_value=0.0, max_value=1.5, value=0.3, step=0.05)
    max_new_tokens = st.slider("Max new tokens", min_value=32, max_value=1024, value=200, step=16)
    do_sample = st.checkbox("Enable sampling", value=True, help="Turn off for more deterministic output.")
    set_seed = st.checkbox("Set random seed", value=False)
    seed = st.number_input("Seed", min_value=0, value=42, step=1, disabled=not set_seed)

    st.markdown("---")
    st.caption("💡 Tip: Smaller models run fine on CPU. For CUDA, install a GPU-enabled PyTorch build.")

# -----------------------------
# Inputs
# -----------------------------
language = st.text_input("Programming Language", value="Python", help="e.g., Python, Java, JavaScript, Go, Rust")
num_questions = st.slider("Number of Questions", min_value=5, max_value=30, value=15, step=1)

generate_btn = st.button("Generate Questions")

# -----------------------------
# Prompt: Kept close to your original
# -----------------------------
# Your original template had literal special tokens. We'll retain the style but make num_questions adjustable.
prompt_tmpl = PromptTemplate.from_template(
    "\nGenerate {num_questions} interview questions from the language {language}\n"
)

# -----------------------------
# Cached Model Loader
# -----------------------------
@st.cache_resource(show_spinner=True)
def load_llm(_model_id: str, _temperature: float, _max_new_tokens: int, _do_sample: bool, _seed: int | None):
    """
    Load tokenizer + HF pipeline and wrap with LangChain's HuggingFacePipeline.
    Cached across reruns for speed.
    """
    if _seed is not None:
        # transformers uses 'seed' via torch & RNG; pipeline respects `torch.manual_seed`
        try:
            import torch
            torch.manual_seed(int(_seed))
        except Exception:
            pass

    tokenizer = AutoTokenizer.from_pretrained(_model_id)
    text_gen = pipeline(
        "text-generation",
        model=_model_id,
        tokenizer=tokenizer,
        max_new_tokens=_max_new_tokens,
        temperature=_temperature,
        do_sample=_do_sample,
        # trust_remote_code=False  # set True if a custom model repo requires it
    )
    return HuggingFacePipeline(pipeline=text_gen)

# -----------------------------
# Build the Chain (prompt → llm → parser)
# -----------------------------
def build_chain(llm):
    parser = StrOutputParser()
    chain = prompt_tmpl | llm | parser
    return chain

# -----------------------------
# Main Action
# -----------------------------
if generate_btn:
    with st.spinner("Loading model (first time may take a while) ..."):
        llm = load_llm(
            model_id,
            temperature,
            max_new_tokens,
            do_sample,
            seed if set_seed else None
        )
    chain = build_chain(llm)

    try:
        raw_text = chain.invoke({"language": language.strip(), "num_questions": num_questions})
        # Post-process to bullet points for readability.
        # Many small LMs produce a single blob—let's split by line and keep non-empty.
        lines = [ln.strip(" -•\t") for ln in raw_text.splitlines() if ln.strip()]
        # If the model didn't produce enumerated lines, fall back to the whole text as one block
        if len(lines) <= 1:
            st.subheader("Generated Questions")
            st.write(raw_text)
        else:
            st.subheader("Generated Questions")
            for i, q in enumerate(lines, start=1):
                st.markdown(f"{i}. {q}")
        with st.expander("Show raw model output"):
            st.code(raw_text)
    except Exception as e:
        st.error(f"Generation failed: {e}")

st.markdown("---")
st.caption("Built with Streamlit, LangChain, and Hugging Face Transformers.")