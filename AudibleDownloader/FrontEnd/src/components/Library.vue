<script setup>
import { ref, onMounted, watch } from 'vue'
import { Icon } from '@iconify/vue';
import MergeDialog from './MergeDialog.vue';

var searchTimeout = null;

const isLoading = ref(true);
const libraryItems = ref([]);
const libraryItemsFiltered = ref([]);
const searchText = ref();

const sortDescending = ref(false);

const selectedSort = ref(1);
const sortOptions = ref([
    { name: 'Title', code: 1 },
    { name: 'Author', code: 2 },
    { name: 'Release Date', code: 3 },
    { name: 'Length', code: 4 },
    { name: 'Series', code: 5 },
]);

const onlyDownloaded = ref(false);
const onlyCanDownload = ref(true);

const mergeDialogVisible = ref(false);
const selectedLibraryItem = ref();

watch(
    [searchText, selectedSort, sortDescending, onlyDownloaded, onlyCanDownload],
    () => {
        const searchOptions = {
            searchText: searchText.value,
            selectedSort: selectedSort.value,
            sortDescending: sortDescending.value,
            onlyDownloaded: onlyDownloaded.value,
            onlyCanDownload: onlyCanDownload.value
        };
        localStorage.setItem('searchOptions', JSON.stringify(searchOptions));
    },
    { deep: true }
);

onMounted(async () => {
    await getLibrary();
    getSearchOptions();
    searchLibrary();
    isLoading.value = false;
});

async function getLibrary() {
    isLoading.value = true;
    const libraryResult = await galdrInvoke("getLibrary");
    if (libraryResult && libraryResult.items) {
        libraryItems.value = libraryResult.items;
    }
}

function getSearchOptions() {
    const searchOptions = localStorage.getItem("searchOptions");
    if (searchOptions) {
        try {
            const options = JSON.parse(searchOptions);
            searchText.value = options.searchText || null;
            selectedSort.value = options.selectedSort || 1;
            sortDescending.value = options.sortDescending || false;
            onlyDownloaded.value = options.onlyDownloaded || false;
            onlyCanDownload.value = options.onlyCanDownload ?? true; // Use ?? to preserve true as default
        } catch (error) {
            console.error('Failed to parse search options:', error);
        }
    }
}

function searchLibrary() {
    let results = [...libraryItems.value];

    // Step 1: Apply search filter
    if (searchText.value && searchText.value.trim() !== '') {
        const search = searchText.value.toLowerCase().trim();
        results = results.filter(item => {
            // Search in title and subtitle
            const titleMatch = item.title?.toLowerCase().includes(search) ||
                item.subtitle?.toLowerCase().includes(search);

            // Search in authors
            const authorMatch = item.authors?.some(author =>
                author.toLowerCase().includes(search)
            );

            // Search in narrators
            const narratorMatch = item.narrators?.some(narrator =>
                narrator.toLowerCase().includes(search)
            );

            // Search in all series titles
            const seriesMatch = item.series?.some(s =>
                s.title?.toLowerCase().includes(search)
            );

            return titleMatch || authorMatch || narratorMatch || seriesMatch;
        });
    }

    // Step 2: Apply secondary filters
    if (onlyDownloaded.value) {
        results = results.filter(item => item.isDownloaded);
    }

    if (onlyCanDownload.value) {
        results = results.filter(item => item.isPlayable);
    }

    // Step 3: Apply sorting
    const direction = sortDescending.value ? -1 : 1;

    switch (selectedSort.value) {
        case 1: // Title
            results.sort((a, b) => {
                const titleCompare = (a.title || '').localeCompare(b.title || '') * direction;
                if (titleCompare !== 0) return titleCompare;

                // Secondary sort by series sequence if both have sequential series
                const aSeq = a.series?.find(s => s.sequence >= 0);
                const bSeq = b.series?.find(s => s.sequence >= 0);
                if (aSeq && bSeq && aSeq.title === bSeq.title) {
                    return (aSeq.sequence - bSeq.sequence) * direction;
                }
                return 0;
            });
            break;

        case 2: // Author
            results.sort((a, b) => {
                const aAuthor = a.authors?.[0] || '';
                const bAuthor = b.authors?.[0] || '';
                return aAuthor.localeCompare(bAuthor) * direction;
            });
            break;

        case 3: // Release Date
            results.sort((a, b) => {
                const aDate = new Date(a.releaseDate);
                const bDate = new Date(b.releaseDate);
                return (aDate - bDate) * direction;
            });
            break;

        case 4: // Length
            results.sort((a, b) => {
                return ((a.runtimeMinutes || 0) - (b.runtimeMinutes || 0)) * direction;
            });
            break;

        case 5: // Series
            results.sort((a, b) => {
                // Get first sequential series for each item
                const aSeqSeries = a.series?.find(s => s.sequence >= 0);
                const bSeqSeries = b.series?.find(s => s.sequence >= 0);

                // Items without sequential series come first, sorted by title
                if (!aSeqSeries && !bSeqSeries) {
                    return (a.title || '').localeCompare(b.title || '') * direction;
                }
                if (!aSeqSeries) return -1 * direction;
                if (!bSeqSeries) return 1 * direction;

                // Both have sequential series - group by series title
                const seriesCompare = aSeqSeries.title.localeCompare(bSeqSeries.title) * direction;
                if (seriesCompare !== 0) return seriesCompare;

                // Same series - sort by sequence
                return (aSeqSeries.sequence - bSeqSeries.sequence) * direction;
            });
            break;
    }

    libraryItemsFiltered.value = results;
}

async function startBookDownload(item) {
    item.downloadProgress = 0.001;

    const preferredCodecs = [
        'LC_128_44100_stereo',
        'LC_128_22050_stereo',
        'LC_64_44100_stereo',
        'LC_64_22050_stereo',
        'LC_32_22050_stereo',
        'aax',
        'mp42264',
        'mp42232',
        'format4'
    ];

    let codec = null;
    for (const preferred of preferredCodecs) {
        if (item.availableCodecs.some(c => c === preferred)) {
            codec = preferred;
            break;
        }
    }

    if (!codec && item.availableCodecs.length > 0) {
        codec = item.availableCodecs[0];
    }

    const promise = galdrInvoke("downloadBook", { libraryEntry: item, codec: codec });

    const intervalId = setInterval(async () => {
        try {
            const result = await galdrInvoke("getDownloadProgress", { asin: item.asin });
            item.downloadProgress = result.progress || 0.001;
            item.downloadMessage = result?.message;

            if (result.progress >= 1.0 || result.progress === undefined) {
                clearInterval(intervalId);

                if (result.progress >= 1.0) {
                    galdrInvoke("clearDownloadProgress", { asin: item.asin });
                    item.downloadProgress = null;
                    item.downloadMessage = null;
                }
            }
        } catch (error) {
            console.error("Failed to get progress:", error);
            clearInterval(intervalId);
        }
    }, 500);

    const downloadResult = await promise;
    if (downloadResult.success) {
        item.downloadProgress = null;
        item.downloadMessage = null;
        item.isDownloaded = true;
        item.directory = downloadResult.directory;
        item.isMerged = downloadResult.isMerged;
    }
}

function minutesToDisplay(minutes) {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours === 0) {
        return `${mins}m`;
    }
    return `${hours}h ${mins}m`;
}

function openDirectory(item) {
    galdrInvoke("openDirectory", { directory: item.directory });
}

function syncAll() {

}

function toggleSort() {
    sortDescending.value = !sortDescending.value;
    searchLibrary();
}

function debounceSearch() {
    if (searchTimeout)
        clearTimeout(searchTimeout);

    searchTimeout = setTimeout(() => {
        searchLibrary();
    }, 750);
}

function clearSearchText() {
    searchText.value = null;
    searchLibrary();
}

function showMergeDialog(item) {
    selectedLibraryItem.value = item;
    mergeDialogVisible.value = true;
}

async function startMerge(options) {
    const promise = galdrInvoke("mergeBook", {
        libraryEntry: options.libraryItem,
        trimParts: options.trimParts,
        deleteParts: options.deleteParts
    });

    const intervalId = setInterval(async () => {
        try {
            const result = await galdrInvoke("getMergeProgress", { asin: options.libraryItem.asin });
            options.libraryItem.mergeProgress = result?.progress || 0.001;
            options.libraryItem.mergeMessage = result?.message;

            if (result != null && result != undefined) {
                if ((result.progress >= 1.0 && result.status > 1) || result.progress === undefined) {
                    clearInterval(intervalId);

                    if (result.progress >= 1.0) {
                        galdrInvoke("clearMergeProgress", { asin: options.libraryItem.asin });
                        options.libraryItem.mergeProgress = null;
                        options.libraryItem.mergeMessage = null;
                    }
                }
            }
        } catch (error) {
            console.error("Failed to get progress:", error);
            clearInterval(intervalId);
        }
    }, 500);

    const mergeResult = await promise;
    if (mergeResult.success) {
        options.libraryItem.mergeProgress = null;
        options.libraryItem.mergeMessage = null;
        options.libraryItem.isMerged = true;
    } else {
        options.libraryItem.mergeProgress = null;
        // show alert or something with mergeMessage
    }
}
</script>

<template>
    <div class="flex flex-col gap-1 p-4">
        <div>
            <div class="flex justify-between items-center px-1 gap-3">
                <div class="flex flex-1 items-center gap-3">
                    <p class="text-4xl mb-1">Library</p>

                    <IconField class="w-full">
                        <InputIcon>
                            <Icon icon="material-symbols:search-rounded" width="18" height="18" />
                        </InputIcon>
                        <InputText v-model="searchText" placeholder="Search..." class="w-full" size="small"
                            @update:modelValue="debounceSearch" />
                        <InputIcon v-if="searchText" class="cursor-pointer" @click="clearSearchText">
                            <Icon icon="ic:round-clear" width="18" height="18" />
                        </InputIcon>
                    </IconField>

                    <Button severity="secondary" size="small" variant="outlined" class="!p-0 h-[2.188rem] !w-15"
                        @click="toggleSort">
                        <template #icon>
                            <Icon :icon="sortDescending ? 'mdi:sort-descending' : 'mdi:sort-ascending'" width="18"
                                height="18" />
                        </template>
                    </Button>

                    <FloatLabel class="w-full md:w-40" variant="on">
                        <Select v-model="selectedSort" :options="sortOptions" optionLabel="name" optionValue="code"
                            placeholder="Sort" class="w-full md:w-40" size="small" @change="searchLibrary" />
                        <label for="on_label">Sort By</label>
                    </FloatLabel>
                </div>
                <Divider layout="vertical" class="h-7" />
                <div>
                    <Button label="Sync All" size="small" @click="syncAll" />
                </div>
            </div>

            <div class="flex gap-3">
                <ToggleButton v-model="onlyDownloaded" onLabel="Downloaded" offLabel="Downloaded" size="small"
                    class="mt-3" @change="searchLibrary" />

                <ToggleButton v-model="onlyCanDownload" onLabel="Can Download" offLabel="Can Download" size="small"
                    class="mt-3" @change="searchLibrary" />
            </div>
        </div>

        <Divider />

        <div v-if="isLoading" class="flex flex-col gap-4">
            <Skeleton class="w-full" height="15rem" />
            <Skeleton class="w-full" height="15rem" />
            <Skeleton class="w-full" height="15rem" />
            <Skeleton class="w-full" height="15rem" />
            <Skeleton class="w-full" height="15rem" />
            <Skeleton class="w-full" height="15rem" />
        </div>

        <div v-else v-for="item in libraryItemsFiltered" class="grid grid-cols-5 gap-4 p-4">
            <div class="col-span-1">
                <Image :src="item.imageUrl" :alt="item.title + ' Cover'" height="200" pt:image:class="rounded-lg"
                    preview />
            </div>
            <div class="col-span-3 flex flex-col gap-1">
                <p class="text-2xl">{{ item.fullTitle }}</p>
                <p class="text-base">Runtime: {{ minutesToDisplay(item.runtimeMinutes) }}</p>
                <p class="text-base">{{ item.authorsDisplay }}</p>
                <p class="text-base">{{ item.narratorsDisplay }}</p>
                <p class="text-base">Released: {{ new Date(item.releaseDate).toLocaleDateString() }}</p>
                <div class="text-base" v-html="item.description"></div>
                <div v-if="item.series && item.series.length > 0 && item.series.some(s => s.sequence > 0)"
                    class="flex mt-2 gap-2">
                    <Tag v-for="series in item.series.filter(s => s.sequence > 0)"
                        :value="`${series.title} #${series.sequence}`" severity="secondary" />
                </div>
            </div>
            <div class="flex flex-col justify-start gap-2">
                <Button v-if="!item.isDownloaded" :label="item.downloadProgress ? 'Downloading...' : 'Download'"
                    severity="secondary" :disabled="!item.isPlayable || item.downloadProgress || item.isDownloaded"
                    size="small" @click="startBookDownload(item)"
                    :class="!item.isPlayable || item.downloadProgress || item.isDownloaded ? '!cursor-not-allowed' : ''" />

                <Button v-if="item.isDownloaded" label="Open Directory" severity="secondary" size="small"
                    @click="openDirectory(item)" />

                <Button v-if="item.isDownloaded && !item.isMerged" label="Merge Parts" severity="secondary" size="small"
                    :disabled="item.mergeProgress" :class="item.mergeProgress ? '!cursor-not-allowed' : ''"
                    @click="showMergeDialog(item)" />

                <Tag v-if="item.isDownloaded" severity="success" value="Downloaded" rounded></Tag>
                <Tag v-if="item.isMerged" severity="info" value="Merged" rounded></Tag>

                <div v-if="item.downloadProgress" class="w-full">
                    <ProgressBar :value="Math.floor(item.downloadProgress * 100)" class="custom-progress" />
                    <p v-if="item.downloadMessage" class="text-neutral-500">
                        {{ item.downloadMessage }}
                    </p>
                </div>

                <div v-if="item.mergeProgress" class="w-full">
                    <ProgressBar :value="Math.floor(item.mergeProgress * 100)" class="custom-progress" />
                    <p v-if="item.mergeMessage" class="text-neutral-500">
                        {{ item.mergeMessage }}
                    </p>
                </div>
            </div>
        </div>

        <MergeDialog v-model:visible="mergeDialogVisible" :libraryItem=selectedLibraryItem @committed="startMerge" />
    </div>
</template>

<style scoped>
.addon-button {
    background: var(--p-inputtext-background);
}

.custom-progress :deep(.p-progressbar-value) {
    transition: width 0.1s ease-in-out !important;
}
</style>
