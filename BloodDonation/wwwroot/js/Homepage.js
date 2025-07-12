// Hiệu ứng click vào team-item để mở link mới
window.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.team-item[data-href]').forEach(function(item) {
        item.style.cursor = 'pointer';
        item.addEventListener('click', function(e) {
            var link = item.getAttribute('data-href');
            if (link && link !== '#') {
                window.open(link, '_blank');
            }
        });
    });

    // --- Blood type compatibility animation ---
    function clearArrows() {
        document.querySelectorAll('.blood-bag-img').forEach(img => img.classList.remove('selected'));
        const svg = document.getElementById('blood-svg');
        if (svg) {
            svg.innerHTML = '<defs><marker id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto" markerUnits="strokeWidth"><polygon points="0 0, 10 3.5, 0 7" fill="#22c55e"/></marker></defs>';
        }
    }
    function getBagCenter(type) {
        const el = document.querySelector('.blood-bag-col[data-type="' + type + '"] .blood-bag-img');
        if (!el) return {x:0, y:0};
        const rect = el.getBoundingClientRect();
        const parentRect = document.getElementById('blood-bags-row').getBoundingClientRect();
        return {
            x: rect.left + rect.width/2 - parentRect.left,
            y: rect.top + rect.height/2 - parentRect.top + 10
        };
    }
    function drawArrow(from, to) {
        const svg = document.getElementById('blood-svg');
        if (!svg) return;
        const dx = to.x - from.x;
        const dy = to.y - from.y;
        const cx1 = from.x + dx * 0.3;
        const cy1 = from.y - 40;
        const cx2 = to.x - dx * 0.3;
        const cy2 = to.y - 40;
        const path = document.createElementNS('http://www.w3.org/2000/svg','path');
        path.setAttribute('d', `M${from.x},${from.y} C${cx1},${cy1} ${cx2},${cy2} ${to.x},${to.y}`);
        path.setAttribute('class', 'blood-compat-arrow');
        path.setAttribute('marker-end', 'url(#arrowhead)');
        svg.appendChild(path);
        setTimeout(() => {
            path.style.transition = 'stroke-dashoffset 0.7s cubic-bezier(.4,1.7,.6,1)';
            path.style.strokeDashoffset = 0;
        }, 10);
    }
    // Bảng tương thích nhóm máu
    const bloodCompat = {
        'A': ['A', 'AB'],
        'B': ['B', 'AB'],
        'AB': ['AB'],
        'O': ['A', 'B', 'AB', 'O']
    };
    clearArrows();
    document.querySelectorAll('.blood-bag-img').forEach(img => {
        img.addEventListener('click', function() {
            clearArrows();
            img.classList.add('selected');
            const type = img.id.replace('blood-','');
            const from = getBagCenter(type);
            (bloodCompat[type]||[]).forEach(target => {
                const to = getBagCenter(target);
                drawArrow(from, to);
            });
        });
    });
    window.addEventListener('resize', clearArrows);
}); 