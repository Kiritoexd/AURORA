const container = document.getElementById("topLibros");
let draggedCard = null;

container.addEventListener("dragstart", e => {
    if (e.target.classList.contains("draggable-card")) {
        draggedCard = e.target;
        e.target.style.opacity = "0.5";
    }
});

container.addEventListener("dragend", e => {
    if (e.target.classList.contains("draggable-card")) {
        e.target.style.opacity = "1";
        draggedCard = null;
    }
});

container.addEventListener("dragover", e => {
    e.preventDefault();
    const afterElement = getDragAfterElement(container, e.clientY);
    if (afterElement == null) {
        container.appendChild(draggedCard);
    } else {
        container.insertBefore(draggedCard, afterElement);
    }
});

function getDragAfterElement(container, y) {
    const draggableElements = [...container.querySelectorAll(".draggable-card:not([style*='opacity: 0.5'])")];
    return draggableElements.reduce((closest, child) => {
        const box = child.getBoundingClientRect();
        const offset = y - box.top - box.height / 2;
        if (offset < 0 && offset > closest.offset) {
            return { offset: offset, element: child };
        } else {
            return closest;
        }
    }, { offset: Number.NEGATIVE_INFINITY }).element;
}

function guardarOrden() {
    const cards = document.querySelectorAll(".top-card");
    const orden = Array.from(cards).map((c, i) => ({
        libroId: parseInt(c.dataset.id),
        posicion: i + 1
    }));

    fetch("/Lector/GuardarOrden", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(orden)
    })
        .then(res => res.json())
        .then(data => mostrarToast(data.mensaje))
        .catch(err => mostrarToast("Error al guardar TOP", false));
}
