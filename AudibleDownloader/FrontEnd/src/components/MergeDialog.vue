<script setup>
import { ref } from "vue";

const props = defineProps({
    libraryItem: Object
});

const emit = defineEmits(["committed"]);

const visible = defineModel("visible");
const trimParts = ref(true);
const deleteParts = ref(true);

function shown() {
    // load last settings...
}

function merge() {
    visible.value = false;
    emit("committed", {
        libraryItem: props.libraryItem,
        trimParts: trimParts.value,
        deleteParts: deleteParts.value
    });
}
</script>

<template>
    <Dialog v-model:visible="visible" modal header="Merge Settings" :style="{ width: '40rem' }" :dismissableMask="true"
        @show="shown">
        <div class="flex flex-col gap-3">
            <div class="flex flex-col gap-2">
                <div class="flex items-center gap-2">
                    <Checkbox v-model="trimParts" inputId="trim" binary />
                    <label for="trim"> Trim Audible messages between parts </label>
                </div>

                <div class="flex items-center gap-2">
                    <Checkbox v-model="deleteParts" inputId="deleteParts" binary />
                    <label for="deleteParts"> Delete parts when complete </label>
                </div>
            </div>

            <div class="flex justify-end gap-2">
                <Button class="w-24" type="button" label="Cancel" severity="secondary"
                    @click="visible = false"></Button>
                <Button class="w-24" type="button" label="Merge" @click="merge"></Button>
            </div>
        </div>
    </Dialog>
</template>

<style scoped></style>