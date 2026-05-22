// Типы, соответствующие DTO бэкенда

export interface ServiceTypeSimpleDto {
  id: number;
  name: string;
}

export interface CreateTicketWithDeviceDto {
  deviceFingerprint: string;
  clientName: string;
  clientSurname: string;
  serviceTypeId?: number;
  ipAddress?: string;
  userAgent?: string;
}

export interface MoveTicketBackwardDto {
  steps: number;
}

export interface TicketDto {
  id: number;
  queueSessionId: number;
  ticketNumber: string;
  clientName: string;
  clientSurname: string;
  serviceTypeId?: number;
  serviceTypeName?: string;
  serviceLetter?: string;
  sortOrder: number;
  priorityLevel: number;
  status: string;
  version: number;
  createdAt: string;
  calledAt?: string;
  serviceStartedAt?: string;
  serviceEndedAt?: string;
  servedByUserId?: number;
  servedByUserName?: string;
  cancelReason?: string;
  positionInQueue: number;
}

export interface MyActiveTicketDetailDto extends TicketDto {
  estimatedWaitMinutes?: number;
  totalWaiting: number;
}

export interface CreateTicketResponse {
  ticket: TicketDto;
  sessionToken: string;
}