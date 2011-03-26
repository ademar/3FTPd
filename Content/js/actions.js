
function gotError(response) {
    if ("error" === response.type) {
        //maybe response should be a json pair (element,error_message)
        //so we could display it to the user 
        alert(response.response);
        return true;
    }
    return false;
}

function buildTable(anchor, template, data) {
    isOdd = false;
    $(anchor).empty();
    var trans = template.tmpl(data);
    trans.appendTo(anchor);
}

function gridify(s) {
    var t = $(s).dataTable();
    t.fnClearTable();
}

function loadgrid(s, a, t, d) {

    gridify(s);
    buildTable(a, $(t), d);

    $(s).dataTable({ "bJQueryUI": true, "bDestroy": true, "bPaginate": false,
        "bLengthChange": false,
        "bFilter": false,
        "bSort": false,
        "bInfo": false,
        "bAutoWidth": false
    });
}

function confirmDialog(text, ok, cancel) {

    $("#dialog-confirm").dialog({ buttons: {
        "OK": function () {
            $(this).dialog("close");
            ok()
        },
        Cancel: function () {
            $(this).dialog("close");
        }
    }
    });
    $("#dialog-confirm-text").html(text);
    $("#dialog-confirm").dialog('open');
}

//************* connections *******************

function loadConnections() {
    $.get("connections",
    function (response) {

        $("#uptime").html(response.uptime);
        $("#numberOfProcesses").html(response.numberOfProcesses);

        loadgrid("#connections", '#connections_grid', '#connections_template', response.processes);


    }, "json");
}

function kill(id) {

    $.post("kill", $.toJSON({ id: id }), function (response) {
        if (gotError(response)) return;
        loadConnections();
    });
}

function killAll(id) {

    $.post("killall", $.toJSON({ id: id }), function (response) {
        if (gotError(response)) return;
        loadConnections();
    });
}

//********** users *******************

function loadUsers() {
    $.get("users",
    function (response) {
        loadgrid("#users", '#users_grid', '#users_template', response);
    }, "json");
}

function createNewUser() {

    $.post("newuser", $.toJSON({
        username: $("#username").val(),
        password: $("#password").val(),
        homeDirectory: $("#homeDirectory").val()
    }), function (response) {
        if (gotError(response)) return;
        alert(response);
    });
}

function deleteUser(userId) {

    confirmDialog("Are you sure you wan to delete this user?", function () {
        $.post("deleteuser", $.toJSON({ id: userId }), function (response) {
            if (gotError(response)) return;
            loadUsers();
        });
    }, function () { });
}

function saveUser(userId) {
    $.post("updateuser", $.toJSON({
        id: userId,
        homeDirectory: $("#edit-user-homeDirectory").val()
    }), function (response) {
        if (gotError(response)) return;
        alert(response);
    });
}

function editUser(userId) {

    $.post("getuser", $.toJSON({ id: userId }), function (response) {

        if (gotError(response)) return;

        $("#edit-user-username").val(response.username);
        $("#edit-user-homeDirectory").val(response.homeDirectory);

        $("#edit-user").dialog({
            buttons: {
                "Submit": function () { saveUser(userId); $(this).dialog("close"); },
                "Cancel": function () { $(this).dialog("close"); }
            } 
        });
        $("#edit-user").dialog('open');

    }, "json");
}

function changePassword(userId) {

    $.post("getuser", $.toJSON({ id: userId }), function (response) {

        if (gotError(response)) return;

        $("#change-password-user-username").val(response.username)

        $("#change-password-user").dialog({
            buttons: {
                "Submit": function () { savePassword(userId); $(this).dialog("close"); },
                "Cancel": function () { $(this).dialog("close"); }
            }
        });
        $("#change-password-user").dialog('open')

    }, "json");
}

function savePassword(userId) {
    $.post("changepassword", $.toJSON({
        id: userId,
        password: $("#change-password-password").val()
    }), function (response) {
        if (gotError(response)) return;
        alert(response);
    });
}

//********** sites ************

function loadSites() {

    $.get("sites",
        function (response) {
            loadgrid("#sites", '#sites_grid', '#sites_template', response);
        }, "json");
}

function createNewSite() {

    $.post("newsite", $.toJSON({ name: $("#siteName").val(), ipaddress: $("#ipaddress").val() }), function (response) {
        if (gotError(response)) return;
        loadSites()
    });
}

function deleteSite(siteId) {

    confirmDialog("Are you sure you wan to delete this site?", function () {
        $.post("deletesite", $.toJSON({ id: siteId }), function (response) {
            if (gotError(response)) return;
            loadSites()
        });
    }, function () { });
}

function loadAnonymous() {

    $.get("anonymous",
        function (response) {
            if (response.anonymous = "enabled") {
                $("#anonymousAccessEnabled").checked(true);
            } else {
                $("#anonymousAccessEnabled").checked(false);
            }
        }, "json");
}

/**********************************************************/

function showConnections() {
    loadConnections();
}

function showFTPSites() {
    loadSites();
}

function showConfiguration() {
    loadAnonymous();
}

function showUsers() {
    loadUsers();
}

function actionName(status) {
    if (status == 'running') {
        return "stop";
    }
    return "start";
}

function opSite(action, siteId) {
    $.post("site", $.toJSON({ id: siteId, command: action }), function (response) {
        if (gotError(response)) return;
        loadSites()
    });
    showFTPSites();
}

/***************************** config *******************************/
function updateAdminPassword() {
    $.post("changeadminpassword", $.toJSON({
        parameterValue: $("#adminPassword").val()
    }), function (response) {
        if (gotError(response)) return;
        alert(response);
    });
}

function updateAnonymousAccess() {
    $.post("enableanonymousaccess", $.toJSON({
        parameterValue: $("#anonymousAccess").val()
    }), function (response) {
        if (gotError(response)) return;
        alert(response);
    });
}