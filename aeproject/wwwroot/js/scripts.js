/*!
* Start Bootstrap - Agency v7.0.12 (https://startbootstrap.com/theme/agency)
* Copyright 2013-2023 Start Bootstrap
* Licensed under MIT (https://github.com/StartBootstrap/startbootstrap-agency/blob/master/LICENSE)
*/
//
// Scripts
// 

window.addEventListener('DOMContentLoaded', event => {

    // Navbar shrink function
    var navbarShrink = function () {
        const navbarCollapsible = document.body.querySelector('#mainNav');
        if (!navbarCollapsible) {
            return;
        }
        if (window.scrollY === 0) {
            navbarCollapsible.classList.remove('navbar-shrink')
        } else {
            navbarCollapsible.classList.add('navbar-shrink')
        }
    };

    // Shrink the navbar 
    navbarShrink();

    // Shrink the navbar when page is scrolled
    document.addEventListener('scroll', navbarShrink);

    // Activate Bootstrap scrollspy on the main nav element
    const mainNav = document.body.querySelector('#mainNav');
    if (mainNav) {
        new bootstrap.ScrollSpy(document.body, {
            target: '#mainNav',
            rootMargin: '0px 0px -40%',
        });
    }

    // Collapse responsive navbar when toggler is visible
    const navbarToggler = document.body.querySelector('.navbar-toggler');
    const responsiveNavItems = [].slice.call(
        document.querySelectorAll('#navbarResponsive .nav-link')
    );
    responsiveNavItems.map(function (responsiveNavItem) {
        responsiveNavItem.addEventListener('click', () => {
            if (window.getComputedStyle(navbarToggler).display !== 'none') {
                navbarToggler.click();
            }
        });
    });

    // �ШD�M���ƨ���ܦb album-section ��
    fetch('/Albums/AlbumListPartial')
        .then(response => response.text())
        .then(data => {
            var albumSection = document.getElementById("album-section");
            if (albumSection) {
                albumSection.innerHTML = data;

                // �j�M�بƥ�j�w
                var searchInput = document.getElementById('searchInput');
                if (!searchInput) {
                    console.log('�j�M�إ����');
                    return;
                } else {
                    console.log('�j�M�ا��');
                }

                // �j�w�j�M�ت���J�ƥ�
                searchInput.addEventListener('input', function () {
                    var searchText = this.value.toLowerCase();
                    console.log('Search Text:', searchText);  // �d�ݿ�J�����e

                    var albumCards = document.querySelectorAll('.album-card');
                    albumCards.forEach(function (card) {
                        var albumName = card.getAttribute('data-album-name');
                        console.log('Album Name:', albumName);  // �d�ݨC�i�d�����M��W��

                        if (albumName.includes(searchText)) {
                            card.style.display = 'block';
                        } else {
                            card.style.display = 'none';
                        }
                    });
                });
            }
        })
        .catch(error => console.error('Error loading album list:', error));

    // �����C�I���ɺu�ʨ������m
    var albumLink = document.getElementById("album-link");
    if (albumLink) {
        albumLink.addEventListener("click", function (e) {
            e.preventDefault();
            var albumview = document.getElementById("albumview");
            if (albumview) {
                albumview.scrollIntoView({
                    behavior: "smooth"
                });
            }
        });
    }

    // �B�z�I���d�����u��h�Ա��v���s
    $(document).on('click', '.card-link', function (event) {
        event.preventDefault();
        console.log('showModal');

        var albumId = $(this).data('album-id');
        console.log("Album ID: " + albumId);

        fetch(`/Songs/GetSongsByAlbumId?albumId=${albumId}`)
            .then(response => response.json())
            .then(data => {
                var tbodyContent = '';
                data.forEach(song => {
                    tbodyContent += `
                                        <tr>
                                            <td>${song.songName}</td>
                                            <td>${song.duration}</td>
                                            <td>${song.language}</td>
                                            <td>${song.releaseDate}</td>
                                        </tr>
                                    `;
                });

                var modalBody = document.getElementById("modalBody");
                if (modalBody) {
                    modalBody.innerHTML = tbodyContent;
                }
                $('#albumModal').modal('show');
            })
            .catch(error => {
                console.error('Error loading songs:', error);
                var modalBody = document.getElementById("modalBody");
                if (modalBody) {
                    modalBody.innerHTML = '�L�k���J���ظ�ơC';
                }
            });
    });

    // �B�z��^�������s������޿�
    const homeButton = document.getElementById("backToHome");
    const screenHeight = window.innerHeight; // �ʺA���o�ù�����

    window.addEventListener("scroll", function () {
        if (homeButton) { // �T�O homeButton �s�b
            if (window.scrollY > screenHeight) { // �ưʶW�L�@�ӿù����׮���ܫ��s
                homeButton.classList.add("visible");
            } else {
                homeButton.classList.remove("visible");
            }
        }
    });
});