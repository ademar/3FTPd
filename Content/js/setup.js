function setup() {

    $("#refresh-button-dashboard").button({ icons: { primary: 'ui-icon-refresh'} });
    $("#refresh-button-dashboard").click(function () { showConnections(); })


    $("#create-site").dialog({ autoOpen: false, width: 470, resizable: false, title: "Create New Site", modal: true });
    $("#create-site").dialog({ buttons: { "Submit": function () { createNewSite(); }, "Cancel": function () { $(this).dialog("close"); } } });

    $("#create-user").dialog({ autoOpen: false, width: 470, resizable: false, title: "Create New User", modal: true });
    $("#create-user").dialog({ buttons: { "Submit": function () { createNewUser(); }, "Cancel": function () { $(this).dialog("close"); } } });

    $("#edit-user").dialog({ autoOpen: false, width: 470, resizable: false, title: "Edit User", modal: true });
    


    $("#change-password-user").dialog({ autoOpen: false, width: 470, resizable: false, title: "Change User Password", modal: true });
    //$("#change-password-user").dialog({ buttons: { "Submit": function () { changePassword(); }, "Cancel": function () { $(this).dialog("close"); } } });

    $("#dialog-confirm").dialog({ autoOpen: false, width: 470, resizable: false, title: "Please Confirm", modal: true })

    $("#create-user-button").button();
    $("#create-user-button").click(function () { $("#create-user").dialog('open') });

    $("#create-site-button").button();
    $("#create-site-button").click(function () { $("#create-site").dialog('open') });

    $("#create-site-submit").button();
    $("#create-site-submit").click(function () { createNewSite(); });

    $("#update-anon-submit").button();
    $("#update-anon-submit").click(function () { updateAnonymousAccess(); });

    $("#update-admin-submit").button();
    $("#update-admin-submit").click(function () { updateAdminPassword(); });

    $("#create-user-submit").button();
    $("#create-user-submit").click(function () { createNewUser(); });

    $("#tabs").tabs();

    $('#tabs').bind('tabsselect', function (event, ui) {
        switch (ui.index) {

            case 0: showConnections(); break;
            case 1: showFTPSites(); break;
            case 2: showUsers(); break;
            case 3: showConfiguration(); break;
        }
    });

    showConnections();
}