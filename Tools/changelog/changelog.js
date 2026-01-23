/* Category structure data: category and entry types */

// The default category, and a map of category cl name to filename in Resources/Changelog
const ChangelogsDir = "../../Resources/Changelog/"; // must have trailing /
// IF YOU ARE A FORK, CHANGE THESE!!!!!!!!!!!!
const MainCategory = "TRAUMA";
const MainCategoryPath = "TraumaChangelog.yml";
const CategoryPaths = {
	[MainCategory]: MainCategoryPath,
	WIZDEN: "Changelog.yml",
	GOOB: "GoobChangelog.yml",
    MAPS: "Maps.yml",
    RULES: "Rules.yml",
};

// All allowed entry types and the final string to use in the changelog yml
const AllowedEntries = {
	add: "Add",
	remove: "Remove",
	tweak: "Tweak",
	fix: "Fix",
	bugfix: "Fix", // never seen anyone use these 2 but its SS14.Changelog parity
	bug: "Fix",
};
const FallbackEntryType = "Tweak";

// Returns true if a category is present in CategoryPaths, thus exists
function categoryExists(category) {
	return CategoryPaths[category] !== undefined;
}

// Options for saving the changelog yml files
const YamlOptions = { indent: 2, noArrayIndent: true };

// Dependencies
const fs = require("fs");
const yaml = require("js-yaml");

// Regexes
const HeaderRegex = /^\s*(?::cl:|🆑) *([a-z0-9_\- ,]+)?\s+(.+)/ims; // :cl: or 🆑 [0] followed by optional author name [1] and the changelog [2]
const LineRegex = /\r?\n/;
const CategoryRegex = /^([a-zA-Z]+)\s*:/;
const EntryRegex = /^ *[*-]? *(add|remove|tweak|fix): *([^\n\r]+)\r?$/im; // * or - followed by change type [0] and change message [1]
const CommentRegex = /<!--.*?-->/gs; // HTML comments

// Main function
async function main() {
    const prnum = process.env.PR_NUMBER;
    if (prnum === undefined)
    	throw new Error(`Run from an action`);

	const repo = process.env.GITHUB_REPOSITORY;
    const prUrl = `https://github.com/${repo}/pull/${prnum}`;

    // Get PR details
	const uri = `https://api.github.com/repos/${repo}/pulls/${prnum}`;
	const token = process.env.GITHUB_TOKEN;
	const req = {
    	headers: {
    		Accept: 'application/vnd.github+json'
		}
	};
	if (token !== undefined)
		req.headers.Authorization = "Bearer " + token;
    const res = await fetch(uri, req);
	if (!res.ok)
		throw new Error(`Failed to fetch PR information for #${prnum}: ${res.status}`);

    const { merged_at, body, user } = await res.json();

    // Time is something like 2021-08-29T20:00:00Z
    // Time should be something like 2023-02-18T00:00:00.0000000+00:00
    let time = merged_at;
    if (!time) {
        console.log("Pull request was not merged, skipping");
        return;
    }

    time = time.replace("z", ".0000000+00:00").replace("Z", ".0000000+00:00");

    // Remove comments from the body
    commentlessBody = body.replace(CommentRegex, '');

    // Get author
    const headerMatch = HeaderRegex.exec(commentlessBody);
    if (!headerMatch) {
        console.log("No changelog entry found, skipping");
        return;
    }

    let author = headerMatch[1];
    if (!author) {
        console.log("No author found, setting it to author of the PR\n");
        author = user.login;
    }

    // Get all changes from the changelog body
    const changelog = headerMatch[2];
    const entries = getChanges(changelog);
    if (entries === null)
    	return;

    // Construct changelog yml entries
    // Write changelogs
    for (const category in entries)
    {
    	const changes = entries[category];
    	const path = ChangelogsDir + CategoryPaths[category];
	    const entry = {
	        author: author,
	        changes: changes,
	        id: -1, // set inside writeChangelog
	        time: time,
	        url: prUrl
	    };
	    writeChangelog(path, entry);
    }

    console.log(`Changelog updated with changes from PR #${prnum}`);
}


// Code chunking

// Get all changes from the PR body
function getChanges(body) {
    const entries = {};
    let category = MainCategory;
    let empty = true;

    for (const line of body.split(LineRegex)) {
    	if (line === "")
    		continue;

    	const matchedCat = CategoryRegex.exec(line);
    	if (matchedCat !== null) {
    		const name = matchedCat[1].toUpperCase();
    		if (!categoryExists(name))
    		{
    			console.log("Invalid changelog category:", name);
    			continue;
			}

    		category = name;
    		continue;
    	}

    	const match = EntryRegex.exec(line);
    	if (match === null) {
    		console.log("Invalid line in changelog:", line);
    		continue;
    	}

        (entries[category] ??= [])
        	.push({ type: match[1], message: match[2] });
        empty = false;
    }

    if (empty)
    {
        console.log("No changes found, skipping");
        return null;
    }

    // Check change types to finish the entries
    for (const category in entries) {
    	const changes = entries[category];
    	for (const entry of changes) {
    		// map dev-facing string to the yml entry string
    		entry.type = AllowedEntries[entry.type] ?? FallbackEntryType;
        }
    }

    return entries;
}

// Get the highest changelog number from a list of entries
function getHighestCLNumber(entries) {
    let max = 0;
    for (const entry of entries) {
    	const id = entry.id;
    	if (id > max)
    		max = id;
	}

	return max;
}

// Append a changelog entry to a given file
function writeChangelog(path, entry) {
    if (!fs.existsSync(path)) {
    	console.log('skipping nonexistent changelog: ', path);
    	return;
    }

    const file = fs.readFileSync(path, "utf8");
    const data = yaml.load(file);

	entry.id = getHighestCLNumber(data.Entries) + 1;

    console.log('entry (line 183): ', entry);
    console.log('data (line 184): ', data);

    data.Entries.push(entry);

    // Write updated changelogs file
    fs.writeFileSync(
        path,
        yaml.dump(data, YamlOptions).replace(/^---/, "")
    );
}

// Run main
main();
