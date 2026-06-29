import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Input,
  OnInit,
  forwardRef,
  inject,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { QuillModule, type QuillModules } from 'ngx-quill';
import type { ContentChange } from 'ngx-quill';
import type Quill from 'quill';

// Rich authoring toolbar shared by every cce-rich-text-editor instance
// (news, events, resources, country requests, state profile, …).
// The `image` button uploads via the consumer-supplied `imageUploader`
// (asset API) and inserts the returned URL — NOT a base64 blob — so the
// stored HTML stays small. Without an uploader it falls back to Quill's
// default base64 behaviour.
const RICH_TOOLBAR: QuillModules = {
  toolbar: [
    [{ header: [1, 2, 3, 4, false] }],
    ['bold', 'italic', 'underline', 'strike'],
    ['blockquote', 'code-block'],
    [{ list: 'ordered' }, { list: 'bullet' }],
    [{ indent: '-1' }, { indent: '+1' }],
    [{ align: [] }],
    [{ script: 'sub' }, { script: 'super' }],
    [{ direction: 'rtl' }],
    ['link', 'image'],
    ['clean'],
  ],
};

// Allowed content formats — deliberately EXCLUDES `color` and `background`
// (font/bg colours) so they can never enter the content, even via paste or
// pre-existing HTML. Must list every format the toolbar above can produce.
const RICH_FORMATS: string[] = [
  'header',
  'bold',
  'italic',
  'underline',
  'strike',
  'blockquote',
  'code-block',
  'list',
  'indent',
  'align',
  'script',
  'direction',
  'link',
  'image',
];

@Component({
  selector: 'cce-rich-text-editor',
  standalone: true,
  imports: [QuillModule, FormsModule],
  template: `
    <div
      class="cce-rte__wrapper"
      [class.cce-rte--disabled]="isDisabled"
      [class.cce-rte--rtl]="dir === 'rtl'"
      [class.cce-rte--ltr]="dir === 'ltr'"
    >
      @if (label) {
        <label class="cce-rte__label">{{ label }}</label>
      }
      <quill-editor
        [modules]="modules"
        [formats]="formats"
        [placeholder]="placeholder"
        [readOnly]="isDisabled"
        [ngModel]="value"
        (onContentChanged)="onContentChanged($event)"
        (onEditorCreated)="onEditorCreated($event)"
        (onBlur)="onTouched()"
        format="html"
      />
      @if (maxLength) {
        <span class="cce-rte__counter" [class.cce-rte__counter--over]="value.length > maxLength">
          {{ value.length }} / {{ maxLength }}
        </span>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }

    .cce-rte__wrapper {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .cce-rte__label {
      font-size: 12px;
      color: rgba(0, 0, 0, 0.6);
      padding: 0 2px;
      font-family: inherit;
    }

    /* Char counter — counts the HTML payload length, matching the
       server-side limit which validates the raw string. */
    .cce-rte__counter {
      align-self: flex-end;
      font-size: 11px;
      color: rgba(0, 0, 0, 0.55);
      padding: 0 2px;
    }

    .cce-rte__counter--over {
      color: var(--danger--600);
      font-weight: 600;
    }

    /* Match Material outline field border colour and radius */
    :host ::ng-deep .ql-toolbar.ql-snow {
      border: 1px solid rgba(0, 0, 0, 0.38);
      border-bottom: none;
      border-radius: 4px 4px 0 0;
      background: var(--neutrals--50);
      font-family: inherit;
    }

    :host ::ng-deep .ql-container.ql-snow {
      border: 1px solid rgba(0, 0, 0, 0.38);
      border-radius: 0 0 4px 4px;
      font-size: 14px;
      font-family: inherit;
    }

    :host ::ng-deep .ql-editor {
      min-height: 120px;
      padding: 10px 12px;
      line-height: 1.6;
      color: var(--color-text-primary);
    }

    :host ::ng-deep .ql-editor.ql-blank::before {
      color: rgba(0, 0, 0, 0.42);
      font-style: normal;
    }

    /* Hover — deepen border like Material does */
    :host:hover ::ng-deep .ql-toolbar.ql-snow,
    :host:hover ::ng-deep .ql-container.ql-snow {
      border-color: rgba(0, 0, 0, 0.87);
    }

    /* Forced content direction — overrides the document direction so an
       Arabic-content field is RTL even in an LTR UI and vice versa. */
    .cce-rte--rtl ::ng-deep .ql-editor {
      direction: rtl;
      text-align: right;
    }

    .cce-rte--ltr ::ng-deep .ql-editor {
      direction: ltr;
      text-align: left;
    }

    /* Disabled state */
    .cce-rte--disabled ::ng-deep .ql-toolbar.ql-snow,
    .cce-rte--disabled ::ng-deep .ql-container.ql-snow {
      border-color: rgba(0, 0, 0, 0.12);
      background: rgba(0, 0, 0, 0.04);
    }

    .cce-rte--disabled ::ng-deep .ql-editor {
      color: rgba(0, 0, 0, 0.38);
      cursor: not-allowed;
    }

    /* Arabic labels for the header-style picker (Quill renders these via
       CSS ::before). Scoped to RTL so the English UI keeps Quill defaults. */
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-label::before,
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-item::before {
      content: 'عادي' !important;
    }
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-label[data-value="1"]::before,
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-item[data-value="1"]::before {
      content: 'عنوان 1' !important;
    }
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-label[data-value="2"]::before,
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-item[data-value="2"]::before {
      content: 'عنوان 2' !important;
    }
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-label[data-value="3"]::before,
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-item[data-value="3"]::before {
      content: 'عنوان 3' !important;
    }
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-label[data-value="4"]::before,
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-item[data-value="4"]::before {
      content: 'عنوان 4' !important;
    }

    /* In RTL the header picker's dropdown arrow (Quill positions its SVG at
       right:0) collides with the Arabic label. Move the arrow to the left
       and pad the label so the text has clear room. */
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-label {
      padding-right: 8px;
      padding-left: 20px;
    }
    :host-context([dir="rtl"]) ::ng-deep .ql-snow .ql-picker.ql-header .ql-picker-label svg {
      right: auto;
      left: 0;
    }
  `],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RichTextEditorComponent),
      multi: true,
    },
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RichTextEditorComponent implements ControlValueAccessor, OnInit {
  @Input() label = '';
  @Input() placeholder = '';
  /** When set, shows a live character counter (counts the HTML string,
   *  which is what the backend validates against). Pair with a
   *  Validators.maxLength on the form control to actually block submit. */
  @Input() maxLength?: number;

  /** Force the editor CONTENT direction regardless of the page locale —
   *  e.g. dir="rtl" for an Arabic-content field inside an English UI,
   *  or dir="ltr" for an English-content field inside an Arabic UI.
   *  Unset = inherit from the document. */
  @Input() dir?: 'rtl' | 'ltr';

  /** Uploads an image file and resolves to its URL (or null on failure).
   *  When provided, the toolbar image button uploads via this function
   *  (asset API) and inserts the URL — keeping the stored HTML small.
   *  When omitted, the image button falls back to Quill's base64 embed. */
  @Input() imageUploader?: (file: File) => Promise<string | null>;

  private readonly cdr = inject(ChangeDetectorRef);
  private readonly doc = inject(DOCUMENT);

  /** The Quill instance, captured on (onEditorCreated). */
  private quill: Quill | null = null;

  readonly modules: QuillModules = RICH_TOOLBAR;
  readonly formats: string[] = RICH_FORMATS;

  /** Wire the toolbar image button to upload-then-insert-URL when an
   *  `imageUploader` is supplied. */
  onEditorCreated(quill: Quill): void {
    this.quill = quill;
    if (!this.imageUploader) return;
    const toolbar = quill.getModule('toolbar') as {
      addHandler(name: string, handler: () => void): void;
    };
    toolbar.addHandler('image', () => this.pickAndUploadImage());
  }

  private pickAndUploadImage(): void {
    const input = this.doc.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    input.onchange = async () => {
      const file = input.files?.[0];
      if (!file || !this.imageUploader || !this.quill) return;
      const url = await this.imageUploader(file);
      if (!url) return;
      const range = this.quill.getSelection(true);
      const index = range ? range.index : 0;
      this.quill.insertEmbed(index, 'image', url, 'user');
      this.quill.setSelection(index + 1, 0);
    };
    input.click();
  }

  /** Quill's stylesheet is shipped as a lazy `quill` style bundle (see
   *  project.json) instead of the global render-blocking bundle. Inject it
   *  once, the first time any editor instance mounts. (PERF001 A1) */
  ngOnInit(): void {
    const id = 'cce-quill-css';
    if (this.doc.getElementById(id)) return;
    const link = this.doc.createElement('link');
    link.id = id;
    link.rel = 'stylesheet';
    link.href = 'quill.css';
    this.doc.head.appendChild(link);
  }

  value = '';
  isDisabled = false;

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  onChange: (v: string) => void = () => {};
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  onTouched: () => void = () => {};

  onContentChanged(event: ContentChange): void {
    const html = event.html ?? '';
    this.value = html;
    this.onChange(html);
  }

  writeValue(value: string): void {
    this.value = value ?? '';
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (v: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
    this.cdr.markForCheck();
  }
}
