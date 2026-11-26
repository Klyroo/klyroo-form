// Optional helpers of Klyroo.Form. Every call is best effort: the components work without them, they only move focus.
const FOCUSABLE = 'input:not([type="hidden"]):not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])';

// Focuses the element with this id, or its first focusable descendant (a radio group or a rating keeps its id on the wrapper).
export function focusById(id) {
  const root = document.getElementById(id);
  if (!root) return false;
  const direct = root.matches(FOCUSABLE) || root.getAttribute('tabindex') === '-1';
  const target = direct ? root : (root.querySelector(FOCUSABLE) ?? root);
  target.focus({ preventScroll: false });
  target.scrollIntoView?.({ block: 'center', behavior: 'instant' });
  return document.activeElement === target;
}
