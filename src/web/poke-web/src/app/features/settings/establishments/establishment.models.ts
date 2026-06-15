export interface AddressDto {
  line1: string | null;
  line2: string | null;
  postalCode: string | null;
  city: string | null;
  country: string | null;
}

export interface EstablishmentDto {
  id: string;
  tenantId: string;
  name: string;
  address: AddressDto;
  phone: string | null;
  email: string | null;
  openingHoursJson: string | null;
  defaultReservationDurationMinutes: number;
  reservationSlotIntervalMinutes: number;
  reservationAdvanceWindowDays: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateEstablishmentRequest {
  name: string;
  address: AddressDto | null;
  phone: string | null;
  email: string | null;
  openingHoursJson: string | null;
}

export interface UpdateEstablishmentRequest {
  name: string;
  address: AddressDto | null;
  phone: string | null;
  email: string | null;
  openingHoursJson: string | null;
  defaultReservationDurationMinutes: number;
  reservationSlotIntervalMinutes: number;
  reservationAdvanceWindowDays: number;
  isActive: boolean;
}
