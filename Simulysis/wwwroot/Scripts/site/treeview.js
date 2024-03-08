$('.treeview-caret').click(function toggle() {
    $(this)
        .toggleClass('treeview-caret--active')
        .closest('li')
        .find('> .treeview-nested')
        .toggleClass('treeview-nested--active')
})

$('.treeview li:not(.treeview-expand)').click(function navigate() {
    $(this).find('> a')[0].click()
})