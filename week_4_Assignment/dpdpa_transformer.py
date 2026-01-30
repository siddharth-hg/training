import streamlit as st
import numpy as np
from sentence_transformers import SentenceTransformer
from transformers import pipeline
from datasets import load_dataset

#configurations
PDF_DIR = "pdffolder"          # folder containing your PDF(s)
TOP_K = 3                     # number of chunks to retrieve
CHUNK_SIZE = 900              # characters per chunk
CHUNK_OVERLAP = 150           # overlap to preserve continuity


#Model Loading
@st.cache_resource
def load_models():
    embed_model = SentenceTransformer("all-MiniLM-L6-v2")

    llm = pipeline(
        task="text-generation",                          
        model="TinyLlama/TinyLlama-1.1B-Chat-v1.0",         # CHANGED (instruction/chat)
        max_new_tokens=256,
        do_sample=False,                                   # reduces randomness + looping
        repetition_penalty=1.15,                            # anti-repeat
        no_repeat_ngram_size=4,                             # anti-repeat
        return_full_text=False                              # only new tokens, not full prompt
    )
    return embed_model, llm

embed_model, llm = load_models()


# ----------------------------
# LOAD PDF -> EXTRACT TEXT -> CHUNK
# ----------------------------
@st.cache_resource
def load_pdf_text_chunks():
    ds = load_dataset("pdffolder", data_dir=PDF_DIR, split="train")

    all_text = []
    for row in ds:
        pdf_obj = row["pdf"]
        text = "\n".join([(p.extract_text() or "") for p in pdf_obj.pages])
        if text.strip():
            all_text.append(text)

    full_text = "\n\n".join(all_text).strip()

    # Simple character-based chunking
    chunks = []
    start = 0
    step = CHUNK_SIZE - CHUNK_OVERLAP
    while start < len(full_text):
        chunk = full_text[start:start + CHUNK_SIZE]
        if chunk.strip():
            chunks.append(chunk)
        start += step

    return chunks

docs = load_pdf_text_chunks()


# ----------------------------
# EMBEDDINGS (CACHED)
# ----------------------------
@st.cache_resource
def build_embeddings(docs_list):
    emb = embed_model.encode(docs_list, convert_to_numpy=True)
    # Normalize for cosine similarity
    emb = emb / (np.linalg.norm(emb, axis=1, keepdims=True) + 1e-10)
    return emb

doc_embeddings = build_embeddings(docs)


# ----------------------------
# RETRIEVER
# ----------------------------
def retrieve(query, top_k=TOP_K):
    q = embed_model.encode([query], convert_to_numpy=True)[0]
    q = q / (np.linalg.norm(q) + 1e-10)
    scores = doc_embeddings @ q
    idx = np.argsort(scores)[-top_k:][::-1]
    return [docs[i] for i in idx], [float(scores[i]) for i in idx]


# ----------------------------
# RAG GENERATION
# ----------------------------
def generate_answer(query):
    retrieved_docs, scores = retrieve(query)

    # Keep context short to prevent generation issues
    context = "\n\n---\n\n".join(retrieved_docs)
    context = context[:3500]
    prompt = f"""<s>[INST]
You are a compliance assistant.
Answer ONLY using the context below.
If the answer is not in the context, say exactly: Not found in the document.

Context:
{context}

Question:
{query}
[/INST]
Answer:
"""

    out = llm(prompt)[0]["generated_text"].strip()
    if len(out) > 1200:
        out = out[:1200].rsplit(".", 1)[0] + "."

    return out, retrieved_docs, scores


# ----------------------------
# STREAMLIT UI
# ----------------------------
st.set_page_config(page_title="RAG Chatbot", page_icon="🤖")
st.title("🤖 RAG Chatbot (PDF + RAG)")
st.write("Ask questions based on the uploaded DPDP reference PDF.")

user_query = st.text_input("Enter your question:")

if user_query:
    with st.spinner("Thinking..."):
        answer, sources, scores = generate_answer(user_query)

    st.subheader("🧠 Answer")
    st.write(answer)

    st.subheader("📚 Retrieved Context")
    for i, (src, sc) in enumerate(zip(sources, scores), start=1):
        st.markdown(f"**{i}. Score:** `{sc:.3f}`")
        st.code(src[:1000] + ("..." if len(src) > 1000 else ""))