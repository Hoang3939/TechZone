// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    // Add event listeners to category items if they exist
    const categoryItems = document.querySelectorAll(".category-item")
    if (categoryItems) {
        categoryItems.forEach((item) => {
            item.addEventListener("click", function (e) {
                if (e.target.tagName.toLowerCase() === "i" || e.target === this) {
                    e.preventDefault()
                    const categoryId = this.getAttribute("data-category-id")
                    const subcategoryList = document.getElementById(`subcategory-${categoryId}`)

                    if (subcategoryList) {
                        // Toggle active class
                        subcategoryList.classList.toggle("active")

                        // Toggle icon rotation
                        const icon = this.querySelector("i")
                        if (icon) {
                            icon.style.transform = subcategoryList.classList.contains("active") ? "rotate(180deg)" : "rotate(0)"
                        }
                    }
                }
            })
        })
    }

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    if (tooltipTriggerList.length > 0) {
        tooltipTriggerList.map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl))
    }

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
    if (popoverTriggerList.length > 0) {
        popoverTriggerList.map((popoverTriggerEl) => new bootstrap.Popover(popoverTriggerEl))
    }
})
